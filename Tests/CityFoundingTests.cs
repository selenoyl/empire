using Game.CoreSim;

namespace Tests;

public class CityFoundingTests
{
    [Fact]
    public void CannotFoundWithinDistanceThree()
    {
        var cities = new[] { new City { Id = 1, OwnerId = 1, Pos = new Hex(5, 5) } };
        Assert.False(Rules.CanFoundCity(new Hex(7, 5), cities));
        Assert.True(Rules.CanFoundCity(new Hex(9, 5), cities));
    }
}
