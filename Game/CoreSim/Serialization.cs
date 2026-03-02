namespace Game.CoreSim;

public sealed class SnapshotDto
{
    public int Version { get; init; } = 2;
    public int Turn { get; init; }
    public int ActivePlayerId { get; init; }
    public int Seed { get; init; }
    public string StateHash { get; init; } = "";
    public MatchConfig Config { get; init; } = new();
    public List<TileDto> Tiles { get; init; } = [];
    public List<UnitDto> Units { get; init; } = [];
    public List<CityDto> Cities { get; init; } = [];
    public List<PlayerDto> Players { get; init; } = [];
    public List<DiplomacyDto> Diplomacy { get; init; } = [];
}
public sealed record TileDto(int Q, int R, string Terrain, bool Forest, int? OwnerId, string Improvement, string Resource, bool ResourceActive);
public sealed record UnitDto(int Id, int OwnerId, string Type, int Q, int R, int Hp, bool Fortified);
public sealed record CityDto(int Id, int OwnerId, string Name, int Q, int R, int Pop, int Food, int Production, int Science, int Culture, int BorderLevel, bool IsCapital, string? QueueItem);
public sealed record PlayerDto(int PlayerId, string CivId, bool IsAi, int Gold, int ResearchProgress, int CultureTotal, string? ActiveTech, List<string> UnlockedTechs);
public sealed record DiplomacyDto(int A, int B, string State, bool OpenBorders);

public static class SnapshotMapper
{
    public static SnapshotDto ToDto(SimState s) => new()
    {
        Turn = s.Turn,
        ActivePlayerId = s.ActivePlayerId,
        Seed = s.Seed,
        StateHash = s.ComputeStateHash(),
        Config = s.Config,
        Tiles = s.Map.Tiles.Values.Select(t => new TileDto(t.Tile.Hex.Q, t.Tile.Hex.R, t.Tile.Terrain.ToString(), t.Tile.Forest, t.OwnerId, t.Improvement.ToString(), t.Resource.ToString(), t.ResourceActive)).ToList(),
        Units = s.Units.Values.Select(u => new UnitDto(u.Id, u.OwnerId, u.Type.ToString(), u.Pos.Q, u.Pos.R, u.Hp, u.Fortified)).ToList(),
        Cities = s.Cities.Values.Select(c => new CityDto(c.Id, c.OwnerId, c.Name, c.Pos.Q, c.Pos.R, c.Pop, c.Food, c.Production, c.Science, c.Culture, c.BorderLevel, c.IsCapital, c.QueueItem)).ToList(),
        Players = s.Players.Values.Select(p => new PlayerDto(p.PlayerId, p.CivId, p.IsAi, p.Gold, p.ResearchProgress, p.CultureTotal, p.ActiveTech?.ToString(), p.UnlockedTechs.Select(t => t.ToString()).ToList())).ToList(),
        Diplomacy = s.Diplomacy.Select(d => new DiplomacyDto(d.A, d.B, d.State.ToString(), d.OpenBorders)).ToList()
    };
}
