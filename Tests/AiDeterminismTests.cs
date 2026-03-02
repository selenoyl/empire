using Game.CoreSim;

namespace Tests;

public class AiDeterminismTests
{
    [Fact]
    public void AiProducesDeterministicCommandsForFixedSeed()
    {
        var map = MapGen.Create("Small", 7);
        var s = new SimState(7, map);
        s.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1", IsAi = true };
        s.Players[2] = new PlayerState { PlayerId = 2, CivId = "civ2" };
        s.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        s.Civs["civ2"] = new CivDefinition { Id = "civ2" };
        var sim = new Simulator(s);
        sim.SpawnInitialUnits(1, new Hex(3,3));
        var ai = new AiController();
        var a = ai.GenerateTurnCommands(s, 1).Select(c => c.GetType().Name).ToArray();
        var b = ai.GenerateTurnCommands(s, 1).Select(c => c.GetType().Name).ToArray();
        Assert.Equal(a, b);
    }
}
