using System.Collections.Concurrent;
using System.Text;

namespace Game.Diagnostics;

public static class DebugTrace
{
    private static readonly ConcurrentQueue<string> RecentEntries = new();
    private static readonly object Gate = new();
    private static StreamWriter? _writer;

    public static bool Enabled { get; private set; }

    public static void Initialize(bool enabled)
    {
        Enabled = enabled;
        if (!enabled) return;

        Directory.CreateDirectory("logs");
        var path = Path.Combine("logs", $"debug_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");
        _writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
        Record("debug", "Debug trace initialized");
    }

    public static void Record(string category, string message)
    {
        if (!Enabled) return;
        var line = $"{DateTime.UtcNow:O} [{category}] {message}";

        RecentEntries.Enqueue(line);
        while (RecentEntries.Count > 400 && RecentEntries.TryDequeue(out _)) { }

        lock (Gate)
        {
            _writer?.WriteLine(line);
        }
    }

    public static void RecordException(string category, Exception ex)
        => Record(category, ex.ToString());

    public static IReadOnlyList<string> SnapshotRecent(int max = 80)
        => RecentEntries.Reverse().Take(max).ToList();
}
