using System.Security.Cryptography;
using System.Text;

namespace Game.CoreSim;

public sealed class MapData
{
    public int Width { get; init; }
    public int Height { get; init; }
    public Dictionary<Hex, TileState> Tiles { get; } = [];
    public bool InBounds(Hex h) => h.Q >= 0 && h.R >= 0 && h.Q < Width && h.R < Height;
}

public sealed class SimState
{
    public int Turn { get; set; } = 1;
    public int ActivePlayerId { get; set; } = 1;
    public int Seed { get; init; }
    public MatchConfig Config { get; set; } = new();
    public Random Rng { get; }
    public MapData Map { get; }
    public Dictionary<int, PlayerState> Players { get; } = [];
    public Dictionary<int, Unit> Units { get; } = [];
    public Dictionary<int, City> Cities { get; } = [];
    public Dictionary<string, CivDefinition> Civs { get; } = [];
    public List<string> CommandLog { get; } = [];
    public List<TradeDeal> Trades { get; } = [];
    public List<DiplomacyRelation> Diplomacy { get; } = [];
    public List<string> EventLog { get; } = [];

    public SimState(int seed, MapData map) { Seed = seed; Map = map; Rng = new Random(seed); }

    public string ComputeStateHash()
    {
        var sb = new StringBuilder();
        sb.Append("v2|").Append(Config.GameVersion).Append('|').Append(Turn).Append('|').Append(ActivePlayerId);
        foreach (var p in Players.Values.OrderBy(x => x.PlayerId))
            sb.Append($"|P{p.PlayerId}:{p.CivId}:{p.IsAi}:{p.Gold}:{p.ResearchProgress}:{p.CultureTotal}:{(p.ActiveTech?.ToString() ?? "-")}")
              .Append(string.Join(',', p.UnlockedTechs.OrderBy(x => x).Select(x => x.ToString())));
        foreach (var t in Map.Tiles.OrderBy(x => x.Key.Q).ThenBy(x => x.Key.R))
            sb.Append($"|T{t.Key.Q},{t.Key.R}:{t.Value.Tile.Terrain}:{t.Value.Tile.Forest}:{t.Value.OwnerId?.ToString() ?? "-"}:{t.Value.Improvement}:{t.Value.Resource}:{t.Value.ResourceActive}");
        foreach (var u in Units.Values.OrderBy(x => x.Id))
            sb.Append($"|U{u.Id}:{u.OwnerId}:{u.Type}:{u.Pos.Q},{u.Pos.R}:{u.Hp}:{u.Fortified}");
        foreach (var c in Cities.Values.OrderBy(x => x.Id))
            sb.Append($"|C{c.Id}:{c.OwnerId}:{c.Name}:{c.Pos.Q},{c.Pos.R}:{c.Pop}:{c.Food}:{c.Production}:{c.Science}:{c.Culture}:{c.BorderLevel}:{c.IsCapital}:{c.QueueItem ?? "-"}");
        foreach (var d in Diplomacy.OrderBy(x => x.A).ThenBy(x => x.B))
            sb.Append($"|D{d.A},{d.B}:{d.State}:{d.OpenBorders}");
        foreach (var tr in Trades.OrderBy(x => x.FromPlayerId).ThenBy(x => x.ToPlayerId))
            sb.Append($"|R{tr.FromPlayerId},{tr.ToPlayerId}:{tr.GoldPerTurn}:{tr.TurnsRemaining}");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
