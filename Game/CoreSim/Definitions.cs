namespace Game.CoreSim;

public sealed class CivDefinition
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string LeaderName { get; init; } = "";
    public string PrimaryColor { get; init; } = "#FFFFFF";
    public string SecondaryColor { get; init; } = "#000000";
    public string Emblem { get; init; } = "";
    public string UniqueAbilityId { get; init; } = "";
    public string UniqueUnitOrBuildingId { get; init; } = "";
    public List<ModifierDef> Modifiers { get; init; } = [];
}

public sealed class ModifierDef
{
    public string Target { get; init; } = "UnitStats";
    public string Operation { get; init; } = "add";
    public string Key { get; init; } = "";
    public int Value { get; init; }
}

public enum UnitType { Settler, Warrior, Scout, Archer, Builder, Swordsman }
public enum TechType { Mining, Archery, Masonry, Writing, BronzeWorking, Sailing, Wheel, Agriculture, IronWorking, Pottery, Mathematics, Currency, Engineering, HorsebackRiding, Construction, Education, Philosophy, Machinery, Banking, Astronomy }
public enum ImprovementType { None, Farm, Mine, LumberMill }
public enum ResourceType { None, Wheat, Iron, Horse }
public enum DiplomacyState { War, Peace }
public enum VictoryType { None, Domination, Science, Culture, Score }

public sealed class Unit
{
    public int Id { get; init; }
    public int OwnerId { get; init; }
    public UnitType Type { get; init; }
    public Hex Pos { get; set; }
    public int Hp { get; set; } = 100;
    public bool Fortified { get; set; }
    public int MovePoints { get; set; }
}

public sealed class City
{
    public int Id { get; init; }
    public int OwnerId { get; init; }
    public string Name { get; set; } = "City";
    public Hex Pos { get; init; }
    public bool IsCapital { get; set; }
    public int Pop { get; set; } = 1;
    public int Food { get; set; }
    public int Production { get; set; }
    public int Science { get; set; }
    public int Culture { get; set; }
    public int BorderLevel { get; set; } = 1;
    public string? QueueItem { get; set; }
}

public sealed class TileState
{
    public required Tile Tile { get; init; }
    public int? OwnerId { get; set; }
    public ImprovementType Improvement { get; set; }
    public ResourceType Resource { get; set; }
    public bool ResourceActive { get; set; }
}

public sealed class PlayerState
{
    public int PlayerId { get; init; }
    public string CivId { get; set; } = "";
    public bool IsAi { get; set; }
    public int Gold { get; set; }
    public int GoldPerTurn { get; set; }
    public int Score { get; set; }
    public int ResearchProgress { get; set; }
    public int CultureTotal { get; set; }
    public TechType? ActiveTech { get; set; }
    public HashSet<TechType> UnlockedTechs { get; } = [];
    public HashSet<Hex> Explored { get; } = [];
    public HashSet<Hex> Visible { get; } = [];
}

public sealed record TradeDeal(int FromPlayerId, int ToPlayerId, int GoldPerTurn, int TurnsRemaining);
public sealed record DiplomacyRelation(int A, int B, DiplomacyState State, bool OpenBorders);

public sealed class MatchConfig
{
    public string MapSize { get; init; } = "Small";
    public int MaxTurns { get; init; } = 250;
    public int ProtocolVersion { get; init; } = 2;
    public string GameVersion { get; init; } = "1.0.0";
}
