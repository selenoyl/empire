using Game.CoreSim;

namespace Tests;

public class VictoryConditionTests
{
    [Fact]
    public void ScoreVictoryAtTurnLimit()
    {
        var map = MapGen.Create("Small", 4);
        var s = new SimState(4, map) { Config = new MatchConfig { MaxTurns = 1 } };
        s.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1" };
        s.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        var sim = new Simulator(s);
        var res = sim.Apply(new EndTurnCommand(1));
        Assert.Equal(VictoryType.Score, res.Victory);
    }
}
