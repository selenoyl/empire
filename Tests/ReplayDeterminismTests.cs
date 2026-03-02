using Game.CoreSim;

namespace Tests;

public class ReplayDeterminismTests
{
    [Fact]
    public void ReplayingSameCommandsProducesSameHash()
    {
        var a = Build();
        var initial = SnapshotMapper.ToDto(a.State);
        a.Apply(new SetResearchCommand(1, TechType.Archery));
        a.Apply(new EndTurnCommand(1));
        var cmds = a.State.CommandLog.ToList();

        var b = Build();
        foreach (var c in ReplayDeserializer.ParseCommands(cmds)) b.Apply(c);

        Assert.Equal(a.State.ComputeStateHash(), b.State.ComputeStateHash());
        Assert.Equal(initial.StateHash, SnapshotMapper.ToDto(Build().State).StateHash);
    }

    private static Simulator Build()
    {
        var map = MapGen.Create("Small", 5);
        var s = new SimState(5, map);
        s.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1" };
        s.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        var sim = new Simulator(s);
        sim.SpawnInitialUnits(1, new Hex(2, 2));
        return sim;
    }
}
