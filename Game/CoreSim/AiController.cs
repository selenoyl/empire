namespace Game.CoreSim;

public sealed class AiController
{
    public IEnumerable<ISimCommand> GenerateTurnCommands(SimState state, int playerId)
    {
        var p = state.Players[playerId];
        if (p.ActiveTech is null)
        {
            var tech = Enum.GetValues<TechType>().FirstOrDefault(t => !p.UnlockedTechs.Contains(t));
            yield return new SetResearchCommand(playerId, tech);
        }

        foreach (var city in state.Cities.Values.Where(c => c.OwnerId == playerId))
        {
            if (string.IsNullOrEmpty(city.QueueItem))
                yield return new SetCityProductionCommand(playerId, city.Id, UnitType.Warrior.ToString());
        }

        var settler = state.Units.Values.FirstOrDefault(u => u.OwnerId == playerId && u.Type == UnitType.Settler);
        if (settler is not null && Rules.CanFoundCity(settler.Pos, state.Cities.Values))
            yield return new FoundCityCommand(playerId, settler.Id, $"AI City {state.Turn}");

        foreach (var u in state.Units.Values.Where(u => u.OwnerId == playerId && u.Type != UnitType.Settler))
        {
            var enemy = state.Units.Values.Where(e => e.OwnerId != playerId).OrderBy(e => e.Pos.Distance(u.Pos)).FirstOrDefault();
            if (enemy is not null && enemy.Pos.Distance(u.Pos) <= (u.Type == UnitType.Archer ? 2 : 1))
            {
                yield return new AttackCommand(playerId, u.Id, enemy.Id);
                continue;
            }
            var target = state.Map.Tiles.Values.Where(t => Rules.IsPassable(t.Tile) && !p.Explored.Contains(t.Tile.Hex)).OrderBy(t => t.Tile.Hex.Distance(u.Pos)).Select(t => t.Tile.Hex).FirstOrDefault();
            if (target != default && target != u.Pos) yield return new MoveUnitCommand(playerId, u.Id, target);
        }
    }
}
