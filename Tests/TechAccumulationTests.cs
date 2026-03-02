using Game.CoreSim;

namespace Tests;

public class TechAccumulationTests
{
    [Fact]
    public void EndTurnAccumulatesResearchAndUnlocksTech()
    {
        var map = MapGen.Create("Small", 1);
        var state = new SimState(1, map);
        state.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1", ActiveTech = TechType.Archery };
        state.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        state.Cities[1] = new City { Id = 1, OwnerId = 1, Pos = new Hex(1,1), Science = 20 };
        var sim = new Simulator(state);
        sim.Apply(new EndTurnCommand(1));
        Assert.Contains(TechType.Archery, state.Players[1].UnlockedTechs);
    }
}
