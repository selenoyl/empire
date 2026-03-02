using Game.CoreSim;

namespace Tests;

public class CombatDeterminismTests
{
    [Fact]
    public void SameInputsProduceSameDamage()
    {
        var d1 = Rules.DeterministicDamage(UnitType.Warrior, UnitType.Archer, false);
        var d2 = Rules.DeterministicDamage(UnitType.Warrior, UnitType.Archer, false);
        Assert.Equal(d1, d2);
    }
}
