using Game.CoreSim;

namespace Tests;

public class CoreSimMovementTests
{
    [Fact]
    public void MovementCost_RespectsTerrainAndForest()
    {
        Assert.Equal(1, Rules.MovementCost(new TileState { Tile = new Tile(new Hex(0,0), TerrainType.Grass, false) }));
        Assert.Equal(2, Rules.MovementCost(new TileState { Tile = new Tile(new Hex(0,0), TerrainType.Hills, false) }));
        Assert.Equal(2, Rules.MovementCost(new TileState { Tile = new Tile(new Hex(0,0), TerrainType.Grass, true) }));
        Assert.Equal(int.MaxValue, Rules.MovementCost(new TileState { Tile = new Tile(new Hex(0,0), TerrainType.Water, false) }));
    }
}
