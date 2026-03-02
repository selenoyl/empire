using System.Text;
using System.Text.Json;

namespace Game.Net;

public static class Protocol
{
    public const int Version = 2;

    public static async Task WriteAsync(NetworkStream stream, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var len = BitConverter.GetBytes(bytes.Length + 4);
        var ver = BitConverter.GetBytes(Version);
        await stream.WriteAsync(len, ct);
        await stream.WriteAsync(ver, ct);
        await stream.WriteAsync(bytes, ct);
    }

    public static async Task<string> ReadJsonAsync(NetworkStream stream, CancellationToken ct = default)
    {
        var lenBuf = new byte[4];
        await stream.ReadExactlyAsync(lenBuf, ct);
        var len = BitConverter.ToInt32(lenBuf);
        var buffer = new byte[len];
        await stream.ReadExactlyAsync(buffer, ct);
        var ver = BitConverter.ToInt32(buffer, 0);
        if (ver != Version) throw new InvalidOperationException("Protocol version mismatch");
        return Encoding.UTF8.GetString(buffer, 4, len - 4);
    }
}
