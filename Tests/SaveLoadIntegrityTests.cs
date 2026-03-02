using Game.CoreSim;

namespace Tests;

public class SaveLoadIntegrityTests
{
    [Fact]
    public void SaveLoadPreservesStateHash()
    {
        var map = MapGen.Create("Small", 9);
        var s = new SimState(9, map);
        s.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1" };
        s.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        var sim = new Simulator(s);
        sim.SpawnInitialUnits(1, new Hex(2,2));
        var h1 = s.ComputeStateHash();
        var path = Path.Combine(Path.GetTempPath(), "empire_save_test.json");
        SaveLoad.Save(path, sim);
        Assert.True(SaveLoad.TryLoad(path, out var file, out _));
        var restored = BuildFromSnapshot(file!.Snapshot);
        Assert.Equal(h1, restored.State.ComputeStateHash());
    }

    private static Simulator BuildFromSnapshot(SnapshotDto snap)
    {
        var map = new MapData { Width = snap.Tiles.Max(t => t.Q) + 1, Height = snap.Tiles.Max(t => t.R) + 1 };
        foreach (var t in snap.Tiles)
        {
            var h = new Hex(t.Q, t.R);
            map.Tiles[h] = new TileState { Tile = new Tile(h, Enum.Parse<TerrainType>(t.Terrain), t.Forest), OwnerId = t.OwnerId, Improvement = Enum.Parse<ImprovementType>(t.Improvement), Resource = Enum.Parse<ResourceType>(t.Resource), ResourceActive = t.ResourceActive };
        }
        var s = new SimState(snap.Seed, map) { Turn = snap.Turn, ActivePlayerId = snap.ActivePlayerId, Config = snap.Config };
        foreach (var p in snap.Players) s.Players[p.PlayerId] = new PlayerState { PlayerId = p.PlayerId, CivId = p.CivId, IsAi = p.IsAi };
        foreach (var c in snap.Cities) s.Cities[c.Id] = new City { Id = c.Id, OwnerId = c.OwnerId, Name = c.Name, Pos = new Hex(c.Q, c.R), IsCapital = c.IsCapital };
        foreach (var u in snap.Units) s.Units[u.Id] = new Unit { Id = u.Id, OwnerId = u.OwnerId, Type = Enum.Parse<UnitType>(u.Type), Pos = new Hex(u.Q, u.R), Hp = u.Hp, Fortified = u.Fortified };
        return new Simulator(s);
    }
}
