using Game.CoreSim;

namespace Tests;

public class ResourceActivationTests
{
    [Fact]
    public void MineActivatesIron()
    {
        var map = MapGen.Create("Small", 3);
        var s = new SimState(3, map);
        s.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1" };
        s.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        var pos = new Hex(1,1);
        s.Map.Tiles[pos].Resource = ResourceType.Iron;
        s.Units[1] = new Unit { Id = 1, OwnerId = 1, Type = UnitType.Builder, Pos = pos };
        var sim = new Simulator(s);
        sim.Apply(new BuildImprovementCommand(1, 1, ImprovementType.Mine));
        Assert.True(s.Map.Tiles[pos].ResourceActive);
    }
}
