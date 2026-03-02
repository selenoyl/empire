using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Game.CoreSim;
using Game.Diagnostics;

namespace Game.Net;

public sealed class GameServer
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<int, TcpClient> _clients = new();
    private int _nextSpectatorId = -1;
    private readonly Simulator _sim;
    public string Version { get; }

    public GameServer(int port, Simulator sim, string version = "1.0.0")
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _sim = sim;
        Version = version;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _listener.Start();
        while (!ct.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(ct);
            _ = HandleClient(client, ct);
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken ct)
    {
        var ns = client.GetStream();
        var hsJson = await Protocol.ReadJsonAsync(ns, ct);
        var hs = JsonSerializer.Deserialize<HandshakeMsg>(hsJson);
        DebugTrace.Record("net.server", $"handshake recv={hsJson}");

        if (hs is null || hs.ProtocolVersion != Protocol.Version)
        {
            await Protocol.WriteAsync(ns, new ServerEventMsg(false, "Protocol mismatch", _sim.State.ComputeStateHash(), Version, true), ct);
            client.Close();
            return;
        }

        var playerId = hs.Spectator ? _nextSpectatorId-- : AllocateHumanPlayerId();
        if (!hs.Spectator && playerId < 0)
        {
            await Protocol.WriteAsync(ns, new ServerEventMsg(false, "No available human player slots", _sim.State.ComputeStateHash(), Version), ct);
            client.Close();
            return;
        }

        _clients[playerId] = client;
        DebugTrace.Record("net.server", $"client accepted playerId={playerId} spectator={hs.Spectator}");
        await Protocol.WriteAsync(ns, new ServerSnapshotMsg(SnapshotMapper.ToDto(_sim.State)), ct);

        try
        {
            while (client.Connected && !ct.IsCancellationRequested)
            {
                var json = await Protocol.ReadJsonAsync(ns, ct);
                if (json.Contains("Reason"))
                {
                    await Protocol.WriteAsync(ns, new ServerSnapshotMsg(SnapshotMapper.ToDto(_sim.State)), ct);
                    continue;
                }

                var cmd = JsonSerializer.Deserialize<ClientCommandMsg>(json);
                DebugTrace.Record("net.server", $"recv payload={json}");
                if (cmd is null) continue;
                var mismatch = !string.IsNullOrEmpty(cmd.ClientHash) && cmd.ClientHash != _sim.State.ComputeStateHash();
                if (mismatch)
                    await Protocol.WriteAsync(ns, new ServerSnapshotMsg(SnapshotMapper.ToDto(_sim.State)), ct);

                var result = ApplyClientCommand(cmd, playerId);
                var evt = new ServerEventMsg(result.Ok, result.Message, result.StateHash ?? _sim.State.ComputeStateHash(), Version, mismatch, result.Victory);
                foreach (var kvp in _clients)
                {
                    var c = kvp.Value;
                    if (!c.Connected) continue;
                    await Protocol.WriteAsync(c.GetStream(), evt, ct);
                }

                if (!result.Ok)
                    await Protocol.WriteAsync(ns, new ServerSnapshotMsg(SnapshotMapper.ToDto(_sim.State)), ct);

                DebugTrace.Record("net.server", $"cmd result ok={result.Ok} msg={result.Message} mismatch={mismatch}");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal shutdown
        }
        catch (IOException ex)
        {
            DebugTrace.RecordException("net.server.io", ex);
        }
        catch (ObjectDisposedException ex)
        {
            DebugTrace.RecordException("net.server.dispose", ex);
        }
        finally
        {
            _clients.TryRemove(playerId, out _);
            client.Close();
            DebugTrace.Record("net.server", $"client disconnected playerId={playerId}");
        }
    }

    private int AllocateHumanPlayerId()
    {
        var connected = _clients.Keys.Where(id => id > 0).ToHashSet();
        return _sim.State.Players.Values
            .Where(p => !p.IsAi && !connected.Contains(p.PlayerId))
            .OrderBy(p => p.PlayerId)
            .Select(p => p.PlayerId)
            .DefaultIfEmpty(-1)
            .First();
    }

    private CommandResult ApplyClientCommand(ClientCommandMsg msg, int connectionPlayerId)
    {
        if (connectionPlayerId <= 0)
            return new CommandResult(false, "Spectator is read-only");
        if (msg.PlayerId != connectionPlayerId)
            return new CommandResult(false, "Player spoofing rejected");

        ISimCommand? cmd = msg.CommandType switch
        {
            nameof(MoveUnitCommand) => JsonSerializer.Deserialize<MoveUnitCommand>(msg.PayloadJson),
            nameof(FoundCityCommand) => JsonSerializer.Deserialize<FoundCityCommand>(msg.PayloadJson),
            nameof(SetCityProductionCommand) => JsonSerializer.Deserialize<SetCityProductionCommand>(msg.PayloadJson),
            nameof(SetResearchCommand) => JsonSerializer.Deserialize<SetResearchCommand>(msg.PayloadJson),
            nameof(AttackCommand) => JsonSerializer.Deserialize<AttackCommand>(msg.PayloadJson),
            nameof(EndTurnCommand) => JsonSerializer.Deserialize<EndTurnCommand>(msg.PayloadJson),
            nameof(SetFortifyCommand) => JsonSerializer.Deserialize<SetFortifyCommand>(msg.PayloadJson),
            nameof(BuildImprovementCommand) => JsonSerializer.Deserialize<BuildImprovementCommand>(msg.PayloadJson),
            nameof(SetWarPeaceCommand) => JsonSerializer.Deserialize<SetWarPeaceCommand>(msg.PayloadJson),
            nameof(ProposeTradeCommand) => JsonSerializer.Deserialize<ProposeTradeCommand>(msg.PayloadJson),
            nameof(AcceptOpenBordersCommand) => JsonSerializer.Deserialize<AcceptOpenBordersCommand>(msg.PayloadJson),
            _ => null
        };
        return cmd is null ? new CommandResult(false, "Invalid command") : _sim.Apply(cmd);
    }
}
