namespace Game.CoreSim;

public static class MapGen
{
    public static MapData Create(string size, int seed)
    {
        var (w, h) = size switch { "Medium" => (60, 36), "Large" => (80, 48), "Huge" => (120, 80), _ => (40, 24) };
        var map = new MapData { Width = w, Height = h };
        var rng = new Random(seed);
        for (var q = 0; q < w; q++)
        for (var r = 0; r < h; r++)
        {
            var roll = rng.Next(100);
            var t = roll switch
            {
                < 30 => TerrainType.Grass,
                < 52 => TerrainType.Plains,
                < 64 => TerrainType.Desert,
                < 78 => TerrainType.Hills,
                < 92 => TerrainType.Water,
                _ => TerrainType.Mountain
            };
            var forest = (t is TerrainType.Grass or TerrainType.Plains) && rng.Next(100) < 25;
            var resource = ResourceType.None;
            if (t == TerrainType.Grass && rng.Next(100) < 5) resource = ResourceType.Wheat;
            else if (t == TerrainType.Hills && rng.Next(100) < 5) resource = ResourceType.Iron;
            else if (t == TerrainType.Plains && rng.Next(100) < 5) resource = ResourceType.Horse;
            var hpos = new Hex(q, r);
            map.Tiles[hpos] = new TileState { Tile = new Tile(hpos, t, forest), Resource = resource };
        }
        return map;
    }
}
