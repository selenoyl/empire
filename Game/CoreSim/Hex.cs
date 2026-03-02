namespace Game.CoreSim;

public readonly record struct Hex(int Q, int R)
{
    public int S => -Q - R;
    public static readonly Hex[] NeighborDirs = [new(1,0), new(1,-1), new(0,-1), new(-1,0), new(-1,1), new(0,1)];

    public IEnumerable<Hex> Neighbors()
    {
        foreach (var d in NeighborDirs) yield return new Hex(Q + d.Q, R + d.R);
    }

    public int Distance(Hex other) => (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;

    public IEnumerable<Hex> Ring(int radius)
    {
        if (radius == 0) { yield return this; yield break; }
        var hex = new Hex(Q + NeighborDirs[4].Q * radius, R + NeighborDirs[4].R * radius);
        for (var i = 0; i < 6; i++)
            for (var j = 0; j < radius; j++) { yield return hex; hex = new Hex(hex.Q + NeighborDirs[i].Q, hex.R + NeighborDirs[i].R); }
    }

    public static IEnumerable<Hex> Line(Hex a, Hex b)
    {
        var n = a.Distance(b);
        if (n == 0) { yield return a; yield break; }
        for (var i = 0; i <= n; i++)
        {
            var tNum = i;
            var q = ((a.Q * (n - tNum)) + (b.Q * tNum) + n / 2) / n;
            var r = ((a.R * (n - tNum)) + (b.R * tNum) + n / 2) / n;
            yield return new Hex(q, r);
        }
    }
}

public enum TerrainType { Grass, Plains, Desert, Hills, Water, Mountain }
public sealed record Tile(Hex Hex, TerrainType Terrain, bool Forest);
