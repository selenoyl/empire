namespace Game.CoreSim;

public interface ISimCommand { int PlayerId { get; } }
public sealed record MoveUnitCommand(int PlayerId, int UnitId, Hex Target) : ISimCommand;
public sealed record FoundCityCommand(int PlayerId, int UnitId, string CityName) : ISimCommand;
public sealed record SetCityProductionCommand(int PlayerId, int CityId, string ItemId) : ISimCommand;
public sealed record SetResearchCommand(int PlayerId, TechType Tech) : ISimCommand;
public sealed record AttackCommand(int PlayerId, int AttackerUnitId, int DefenderUnitId) : ISimCommand;
public sealed record EndTurnCommand(int PlayerId) : ISimCommand;
public sealed record SetFortifyCommand(int PlayerId, int UnitId, bool Fortify) : ISimCommand;
public sealed record BuildImprovementCommand(int PlayerId, int UnitId, ImprovementType Improvement) : ISimCommand;
public sealed record SetWarPeaceCommand(int PlayerId, int OtherPlayerId, DiplomacyState State) : ISimCommand;
public sealed record ProposeTradeCommand(int PlayerId, int ToPlayerId, int GoldPerTurn, int DurationTurns) : ISimCommand;
public sealed record AcceptOpenBordersCommand(int PlayerId, int OtherPlayerId, bool Enabled) : ISimCommand;

public sealed record CommandResult(bool Ok, string Message, string? StateHash = null, VictoryType Victory = VictoryType.None);
