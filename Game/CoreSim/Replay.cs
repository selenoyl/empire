using System.Text.Json;

namespace Game.CoreSim;

public sealed class ReplayFile
{
    public int ReplayVersion { get; init; } = 1;
    public SnapshotDto InitialSnapshot { get; init; } = new();
    public List<string> Commands { get; init; } = [];
}

public static class ReplayRunner
{
    public static void SaveReplay(string path, Simulator sim, SnapshotDto initial)
    {
        var replay = new ReplayFile { InitialSnapshot = initial, Commands = sim.State.CommandLog.ToList() };
        File.WriteAllText(path, JsonSerializer.Serialize(replay, new JsonSerializerOptions { WriteIndented = true }));
    }
}
