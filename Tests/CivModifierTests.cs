using Game.CoreSim;

namespace Tests;

public class CivModifierTests
{
    [Fact]
    public void AppliesDataDrivenModifier()
    {
        var map = MapGen.Create("Small", 1);
        var s = new SimState(1, map);
        s.Civs["x"] = new CivDefinition { Id = "x", Modifiers = [new ModifierDef { Target = "CityYields", Operation = "add", Key = "Science", Value = 2 }] };
        s.Players[1] = new PlayerState { PlayerId = 1, CivId = "x" };
        Assert.Equal(5, ModifierSystem.Apply(s, 1, "CityYields", "Science", 3));
    }
}
