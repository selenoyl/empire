using Game.CoreSim;

namespace Game.Net;

public sealed record HandshakeMsg(string Name, int ProtocolVersion, bool Spectator = false);
public sealed record LobbyPlayer(int PlayerId, string Name, string CivId, bool Ready, bool IsAi = false);
public sealed record LobbyStateMsg(List<LobbyPlayer> Players, bool CanStart);
public sealed record StartGameMsg(int Seed, string MapSize, Dictionary<int, string> PlayerCivs);
public sealed record ClientCommandMsg(int PlayerId, string CommandType, string PayloadJson, string ClientHash = "");
public sealed record SnapshotRequestMsg(int PlayerId, string Reason);
public sealed record ServerSnapshotMsg(SnapshotDto Snapshot);
public sealed record ServerEventMsg(bool Ok, string Message, string StateHash, string ServerVersion, bool HashMismatch = false, VictoryType Victory = VictoryType.None);
public sealed record HashMismatchMsg(string ClientHash);
