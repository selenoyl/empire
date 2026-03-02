using Game.CoreSim;

namespace Tests;

public class ImprovementYieldTests
{
    [Fact]
    public void FarmIncreasesFoodYield()
    {
        var tile = new TileState { Tile = new Tile(new Hex(0,0), TerrainType.Grass, false), Improvement = ImprovementType.None };
        var baseYield = Rules.Yield(tile);
        tile.Improvement = ImprovementType.Farm;
        var improved = Rules.Yield(tile);
        Assert.True(improved.food > baseYield.food);
    }
}
