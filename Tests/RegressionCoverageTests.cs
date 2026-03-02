using System.Text.Json;
using Game.CoreSim;

namespace Tests;

public class RegressionCoverageTests
{
    [Fact]
    public void Simulator_UsesNextIdsFromExistingState()
    {
        var map = MapGen.Create("Small", 11);
        var state = new SimState(11, map);
        state.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1" };
        state.Civs["civ1"] = new CivDefinition { Id = "civ1" };

        state.Units[10] = new Unit { Id = 10, OwnerId = 1, Type = UnitType.Settler, Pos = new Hex(3, 3) };
        state.Cities[7] = new City { Id = 7, OwnerId = 1, Pos = new Hex(6, 6), Name = "Old" };

        var sim = new Simulator(state);
        var founded = sim.Apply(new FoundCityCommand(1, 10, "New"));

        Assert.True(founded.Ok);
        Assert.Contains(8, sim.State.Cities.Keys);
    }

    [Fact]
    public void EndTurn_ProcessesAiAfterAdvancingActivePlayer()
    {
        var map = MapGen.Create("Small", 12);
        var state = new SimState(12, map);
        state.Players[1] = new PlayerState { PlayerId = 1, CivId = "civ1", IsAi = false };
        state.Players[2] = new PlayerState { PlayerId = 2, CivId = "civ2", IsAi = true };
        state.Civs["civ1"] = new CivDefinition { Id = "civ1" };
        state.Civs["civ2"] = new CivDefinition { Id = "civ2" };

        var sim = new Simulator(state);
        sim.SpawnInitialUnits(1, new Hex(2, 2));
        sim.SpawnInitialUnits(2, new Hex(8, 8));

        var result = sim.Apply(new EndTurnCommand(1));

        Assert.True(result.Ok);
        Assert.Equal(1, sim.State.ActivePlayerId);
        Assert.Equal(2, sim.State.Turn);
    }

    [Fact]
    public void ReplayDeserializer_ParsesExpandedCommandSet()
    {
        var commands = new string[]
        {
            JsonSerializer.Serialize<ISimCommand>(new SetCityProductionCommand(1, 2, "Warrior")),
            JsonSerializer.Serialize<ISimCommand>(new SetFortifyCommand(1, 3, true)),
            JsonSerializer.Serialize<ISimCommand>(new BuildImprovementCommand(1, 4, ImprovementType.Farm)),
            JsonSerializer.Serialize<ISimCommand>(new SetWarPeaceCommand(1, 2, DiplomacyState.Peace)),
            JsonSerializer.Serialize<ISimCommand>(new ProposeTradeCommand(1, 2, 3, 10)),
            JsonSerializer.Serialize<ISimCommand>(new AcceptOpenBordersCommand(1, 2, true)),
        };

        var parsed = ReplayDeserializer.ParseCommands(commands).Select(c => c.GetType()).ToList();

        Assert.Contains(typeof(SetCityProductionCommand), parsed);
        Assert.Contains(typeof(SetFortifyCommand), parsed);
        Assert.Contains(typeof(BuildImprovementCommand), parsed);
        Assert.Contains(typeof(SetWarPeaceCommand), parsed);
        Assert.Contains(typeof(ProposeTradeCommand), parsed);
        Assert.Contains(typeof(AcceptOpenBordersCommand), parsed);
    }
}
