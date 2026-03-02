using Game.CoreSim;

namespace Tests;

public class CultureBordersTests
{
    [Fact]
    public void CultureExpandsBorders()
    {
        var map = MapGen.Create("Small", 2);
        var s = new SimState(2, map);
        s.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1" };
        s.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        s.Cities[1] = new City { Id = 1, OwnerId = 1, Pos = new Hex(5, 5), BorderLevel = 1, Culture = 20 };
        var sim = new Simulator(s);
        sim.Apply(new EndTurnCommand(1));
        Assert.True(s.Cities[1].BorderLevel >= 2);
    }
}
