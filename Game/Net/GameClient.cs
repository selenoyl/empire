using System.Net.Sockets;
using System.Text.Json;
using Game.CoreSim;
using Game.Diagnostics;

namespace Game.Net;

public sealed class GameClient
{
    private readonly TcpClient _client = new();
    public SnapshotDto? Snapshot { get; private set; }
    public string LastServerHash { get; private set; } = "";
    public bool HashMismatch { get; private set; }
    public event Action<ServerEventMsg>? EventReceived;

    public async Task ConnectAsync(string host, int port, string name, bool spectator = false, CancellationToken ct = default)
    {
        await _client.ConnectAsync(host, port, ct);
        var ns = _client.GetStream();
        await Protocol.WriteAsync(ns, new HandshakeMsg(name, Protocol.Version, spectator), ct);

        var firstMessage = await Protocol.ReadJsonAsync(ns, ct);
        DebugTrace.Record("net.client", $"connect firstMessage={firstMessage}");
        if (firstMessage.Contains("\"Snapshot\""))
        {
            Snapshot = JsonSerializer.Deserialize<ServerSnapshotMsg>(firstMessage)?.Snapshot;
        }
        else
        {
            var evt = JsonSerializer.Deserialize<ServerEventMsg>(firstMessage);
            var message = evt?.Message ?? "Connection rejected";
            throw new InvalidOperationException(message);
        }

        _ = ReceiveLoop(ct);
    }

    public async Task SendCommandAsync(ISimCommand cmd, string hash, CancellationToken ct = default)
    {
        var msg = new ClientCommandMsg(cmd.PlayerId, cmd.GetType().Name, JsonSerializer.Serialize(cmd), hash);
        DebugTrace.Record("net.client", $"send {msg.CommandType} p={msg.PlayerId} hash={hash[..Math.Min(8, hash.Length)]}");
        await Protocol.WriteAsync(_client.GetStream(), msg, ct);
    }

    public async Task RequestSnapshotAsync(int playerId, string reason, CancellationToken ct = default)
        => await Protocol.WriteAsync(_client.GetStream(), new SnapshotRequestMsg(playerId, reason), ct);

    private async Task ReceiveLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _client.Connected)
            {
                var json = await Protocol.ReadJsonAsync(_client.GetStream(), ct);
                DebugTrace.Record("net.client", $"recv {json}");
                if (json.Contains("\"Snapshot\"")) Snapshot = JsonSerializer.Deserialize<ServerSnapshotMsg>(json)?.Snapshot;
                else
                {
                    var evt = JsonSerializer.Deserialize<ServerEventMsg>(json)!;
                    LastServerHash = evt.StateHash;
                    HashMismatch = evt.HashMismatch;
                    EventReceived?.Invoke(evt);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal shutdown
        }
        catch (IOException ex)
        {
            DebugTrace.RecordException("net.client.io", ex);
        }
        catch (ObjectDisposedException ex)
        {
            DebugTrace.RecordException("net.client.dispose", ex);
        }
    }
}
