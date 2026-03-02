using System.Text.Json;

namespace Game.CoreSim;

public sealed class SaveFile
{
    public int SaveVersion { get; init; } = 1;
    public int Seed { get; init; }
    public SnapshotDto Snapshot { get; init; } = new();
}

public static class SaveLoad
{
    public static void Save(string path, Simulator sim)
    {
        var file = new SaveFile { Seed = sim.State.Seed, Snapshot = SnapshotMapper.ToDto(sim.State) };
        File.WriteAllText(path, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static bool TryLoad(string path, out SaveFile? file, out string error)
    {
        try
        {
            file = JsonSerializer.Deserialize<SaveFile>(File.ReadAllText(path));
            if (file is null) { error = "Save parse failed"; return false; }
            if (file.SaveVersion != 1) { error = "Unsupported save version"; return false; }
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            file = null;
            error = $"Corrupted save: {ex.Message}";
            return false;
        }
    }
}
