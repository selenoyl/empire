namespace Game.CoreSim;

public static class Rules
{
    public static int BaseMove(UnitType t) => t == UnitType.Scout ? 3 : 2;
    public static bool IsPassable(Tile tile) => tile.Terrain is not (TerrainType.Water or TerrainType.Mountain);

    public static int MovementCost(TileState tile)
    {
        if (!IsPassable(tile.Tile)) return int.MaxValue;
        var cost = tile.Tile.Terrain switch { TerrainType.Hills => 2, _ => 1 };
        if (tile.Tile.Forest) cost += 1;
        if (tile.OwnerId.HasValue) cost = Math.Max(1, cost - 1);
        return cost;
    }

    public static (int food, int prod, int gold, int science, int culture) Yield(TileState tile)
    {
        var y = tile.Tile.Terrain switch
        {
            TerrainType.Grass => (2,0,0,0,0), TerrainType.Plains => (1,1,0,0,0), TerrainType.Hills => (0,2,0,0,0), TerrainType.Desert => (0,0,1,0,0), _ => (0,0,0,0,0)
        };
        if (tile.Tile.Forest) y = (y.Item1 + 1, y.Item2 + 1, y.Item3, y.Item4, y.Item5);
        y = tile.Improvement switch
        {
            ImprovementType.Farm => (y.Item1 + 1, y.Item2, y.Item3, y.Item4, y.Item5),
            ImprovementType.Mine => (y.Item1, y.Item2 + 1, y.Item3, y.Item4, y.Item5),
            ImprovementType.LumberMill => (y.Item1 + 1, y.Item2 + 1, y.Item3, y.Item4, y.Item5),
            _ => y
        };
        if (tile.ResourceActive)
        {
            if (tile.Resource == ResourceType.Wheat) y = (y.Item1 + 1, y.Item2, y.Item3, y.Item4, y.Item5);
            if (tile.Resource == ResourceType.Iron) y = (y.Item1, y.Item2 + 1, y.Item3, y.Item4, y.Item5);
            if (tile.Resource == ResourceType.Horse) y = (y.Item1, y.Item2, y.Item3 + 1, y.Item4, y.Item5);
        }
        return y;
    }

    public static int DeterministicDamage(UnitType attacker, UnitType defender, bool ranged, int flankers = 0, bool attackerFortified = false, bool defenderFortified = false)
    {
        var atk = attacker switch { UnitType.Warrior => 24, UnitType.Archer => 18, UnitType.Scout => 14, UnitType.Swordsman => 30, _ => 0 };
        var def = defender switch { UnitType.Warrior => 18, UnitType.Archer => 12, UnitType.Scout => 10, UnitType.Swordsman => 24, _ => 8 };
        var delta = atk - def + (ranged ? 3 : 0) + flankers * 2 + (attackerFortified ? 2 : 0) - (defenderFortified ? 3 : 0);
        return Math.Clamp(24 + delta / 2, 8, 45);
    }

    public static bool CanFoundCity(Hex at, IEnumerable<City> cities) => cities.All(c => c.Pos.Distance(at) >= 3);

    public static List<Hex> FindPath(MapData map, Hex start, Hex goal)
    {
        var open = new PriorityQueue<Hex, int>();
        var came = new Dictionary<Hex, Hex>();
        var g = new Dictionary<Hex, int> { [start] = 0 };
        open.Enqueue(start, 0);
        while (open.TryDequeue(out var cur, out _))
        {
            if (cur == goal) break;
            foreach (var n in cur.Neighbors())
            {
                if (!map.InBounds(n) || !map.Tiles.TryGetValue(n, out var tile) || !IsPassable(tile.Tile)) continue;
                var ng = g[cur] + MovementCost(tile);
                if (!g.TryGetValue(n, out var old) || ng < old)
                {
                    g[n] = ng;
                    came[n] = cur;
                    open.Enqueue(n, ng + n.Distance(goal));
                }
            }
        }

        if (!came.ContainsKey(goal)) return [];
        var path = new List<Hex>();
        var t = goal;
        path.Add(t);
        while (t != start) { t = came[t]; path.Add(t); }
        path.Reverse();
        return path;
    }

    public static int VisibilityRange(TileState tile, UnitType t)
    {
        var baseRange = t == UnitType.Scout ? 3 : 2;
        if (tile.Tile.Forest) baseRange -= 1;
        return Math.Max(1, baseRange);
    }
}
