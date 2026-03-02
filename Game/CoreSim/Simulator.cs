using System.Text.Json;
using Game.Diagnostics;

namespace Game.CoreSim;

public sealed class Simulator
{
    public SimState State { get; }
    private int _nextUnitId;
    private int _nextCityId;

    public Simulator(SimState state)
    {
        State = state;
        _nextUnitId = State.Units.Count == 0 ? 1 : State.Units.Keys.Max() + 1;
        _nextCityId = State.Cities.Count == 0 ? 1 : State.Cities.Keys.Max() + 1;
    }

    public CommandResult Apply(ISimCommand cmd)
    {
        if (cmd.PlayerId != State.ActivePlayerId)
            return new(false, "Not your turn");

        DebugTrace.Record("sim.cmd", $"apply {cmd.GetType().Name} p={cmd.PlayerId} turn={State.Turn}");

        try
        {
            var res = cmd switch
            {
                MoveUnitCommand m => Move(m),
                FoundCityCommand f => Found(f),
                SetCityProductionCommand p => SetProduction(p),
                SetResearchCommand r => SetResearch(r),
                AttackCommand a => Attack(a),
                SetFortifyCommand sf => Fortify(sf),
                BuildImprovementCommand b => BuildImprovement(b),
                SetWarPeaceCommand wp => SetWarPeace(wp),
                ProposeTradeCommand td => ProposeTrade(td),
                AcceptOpenBordersCommand ob => OpenBorders(ob),
                EndTurnCommand e => EndTurn(e),
                _ => new CommandResult(false, "Unknown command")
            };

            State.CommandLog.Add(JsonSerializer.Serialize(cmd));
            var final = res.Ok ? res with { StateHash = State.ComputeStateHash() } : res;
            DebugTrace.Record("sim.cmd", $"result {cmd.GetType().Name} ok={final.Ok} msg={final.Message} hash={(final.StateHash ?? "-")[..Math.Min(8, (final.StateHash ?? "-").Length)]}");
            return final;
        }
        catch (Exception ex)
        {
            DebugTrace.RecordException("sim.exception", ex);
            return new(false, $"Simulation error: {ex.Message}");
        }
    }

    public CommandResult Move(MoveUnitCommand cmd)
    {
        if (!State.Units.TryGetValue(cmd.UnitId, out var u) || u.OwnerId != cmd.PlayerId) return new(false, "Unit invalid");
        if (!State.Map.Tiles.TryGetValue(cmd.Target, out var t) || !Rules.IsPassable(t.Tile)) return new(false, "Target impassable");
        var path = Rules.FindPath(State.Map, u.Pos, cmd.Target);
        if (path.Count == 0) return new(false, "No path");
        var cost = path.Skip(1).Sum(h => Rules.MovementCost(State.Map.Tiles[h]));
        var maxMp = ModifierSystem.Apply(State, cmd.PlayerId, "UnitStats", $"Move:{u.Type}", Rules.BaseMove(u.Type));
        if (cost > maxMp) return new(false, "Insufficient move points");
        if (path.Skip(1).Any(step => State.Units.Values.Any(ou => ou.OwnerId != cmd.PlayerId && ou.Pos.Distance(step) == 1) && step != cmd.Target)) return new(false, "Zone of control blocks movement");
        u.Pos = cmd.Target;
        return new(true, "Moved");
    }

    CommandResult Found(FoundCityCommand cmd)
    {
        if (!State.Units.TryGetValue(cmd.UnitId, out var u) || u.OwnerId != cmd.PlayerId || u.Type != UnitType.Settler) return new(false, "Need settler");
        if (!Rules.CanFoundCity(u.Pos, State.Cities.Values)) return new(false, "Too close to another city");
        var city = new City { Id = _nextCityId++, OwnerId = cmd.PlayerId, Pos = u.Pos, Name = cmd.CityName, IsCapital = !State.Cities.Values.Any(c => c.OwnerId == cmd.PlayerId) };
        State.Cities[city.Id] = city;
        State.Units.Remove(u.Id);
        ExpandBorders(city);
        return new(true, "City founded");
    }

    CommandResult SetProduction(SetCityProductionCommand cmd)
    {
        if (!State.Cities.TryGetValue(cmd.CityId, out var city) || city.OwnerId != cmd.PlayerId) return new(false, "City invalid");
        city.QueueItem = cmd.ItemId;
        return new(true, "Production set");
    }

    CommandResult SetResearch(SetResearchCommand cmd)
    {
        var p = State.Players[cmd.PlayerId];
        p.ActiveTech = cmd.Tech;
        return new(true, "Research selected");
    }

    CommandResult Fortify(SetFortifyCommand cmd)
    {
        if (!State.Units.TryGetValue(cmd.UnitId, out var u) || u.OwnerId != cmd.PlayerId) return new(false, "Unit invalid");
        u.Fortified = cmd.Fortify;
        return new(true, cmd.Fortify ? "Fortified" : "Unfortified");
    }

    CommandResult BuildImprovement(BuildImprovementCommand cmd)
    {
        if (!State.Units.TryGetValue(cmd.UnitId, out var u) || u.OwnerId != cmd.PlayerId || u.Type != UnitType.Builder) return new(false, "Need builder");
        var tile = State.Map.Tiles[u.Pos];
        tile.Improvement = cmd.Improvement;
        tile.ResourceActive = tile.Resource switch
        {
            ResourceType.Wheat => cmd.Improvement == ImprovementType.Farm,
            ResourceType.Iron => cmd.Improvement == ImprovementType.Mine,
            ResourceType.Horse => cmd.Improvement == ImprovementType.LumberMill || cmd.Improvement == ImprovementType.Farm,
            _ => false
        };
        return new(true, "Improvement built");
    }

    CommandResult Attack(AttackCommand cmd)
    {
        if (!State.Units.TryGetValue(cmd.AttackerUnitId, out var a) || !State.Units.TryGetValue(cmd.DefenderUnitId, out var d)) return new(false, "Invalid units");
        if (a.OwnerId != cmd.PlayerId || d.OwnerId == cmd.PlayerId) return new(false, "Ownership invalid");
        var rel = Relation(a.OwnerId, d.OwnerId);
        if (rel.State != DiplomacyState.War) return new(false, "Not at war");
        var dist = a.Pos.Distance(d.Pos);
        var ranged = a.Type == UnitType.Archer;
        if ((!ranged && dist != 1) || (ranged && dist > 2)) return new(false, "Out of range");
        var flankers = State.Units.Values.Count(u => u.OwnerId == a.OwnerId && u.Id != a.Id && u.Pos.Distance(d.Pos) == 1);
        d.Hp -= Rules.DeterministicDamage(a.Type, d.Type, ranged, flankers, a.Fortified, d.Fortified);
        if (!ranged) a.Hp -= Rules.DeterministicDamage(d.Type, a.Type, false, 0, d.Fortified, a.Fortified) / 2;
        if (d.Hp <= 0) State.Units.Remove(d.Id);
        if (a.Hp <= 0) State.Units.Remove(a.Id);
        return new(true, "Combat resolved");
    }

    CommandResult SetWarPeace(SetWarPeaceCommand cmd)
    {
        UpsertRelation(cmd.PlayerId, cmd.OtherPlayerId, cmd.State, Relation(cmd.PlayerId, cmd.OtherPlayerId).OpenBorders);
        return new(true, $"Diplomacy {cmd.State}");
    }

    CommandResult ProposeTrade(ProposeTradeCommand cmd)
    {
        State.Trades.Add(new TradeDeal(cmd.PlayerId, cmd.ToPlayerId, cmd.GoldPerTurn, cmd.DurationTurns));
        return new(true, "Trade proposed");
    }

    CommandResult OpenBorders(AcceptOpenBordersCommand cmd)
    {
        var rel = Relation(cmd.PlayerId, cmd.OtherPlayerId);
        UpsertRelation(cmd.PlayerId, cmd.OtherPlayerId, rel.State, cmd.Enabled);
        return new(true, cmd.Enabled ? "Open borders enabled" : "Open borders disabled");
    }

    CommandResult EndTurn(EndTurnCommand cmd)
    {
        RunCityAndTechTick(cmd.PlayerId);
        HealUnits(cmd.PlayerId);
        UpdateFog(cmd.PlayerId);
        var max = State.Players.Keys.Max();
        State.ActivePlayerId = State.ActivePlayerId == max ? 1 : State.ActivePlayerId + 1;
        if (State.ActivePlayerId == 1) State.Turn++;
        RunAiIfNeeded();
        if (State.Turn > State.Config.MaxTurns) return new(true, "Turn ended", Victory: VictoryType.Score);
        var victory = EvaluateVictory();
        return new(true, "Turn ended", Victory: victory);
    }

    private void RunCityAndTechTick(int playerId)
    {
        foreach (var deal in State.Trades.ToList())
        {
            State.Players[deal.FromPlayerId].Gold -= deal.GoldPerTurn;
            State.Players[deal.ToPlayerId].Gold += deal.GoldPerTurn;
            State.Trades.Remove(deal);
            if (deal.TurnsRemaining > 1) State.Trades.Add(deal with { TurnsRemaining = deal.TurnsRemaining - 1 });
        }

        foreach (var c in State.Cities.Values.Where(c => c.OwnerId == playerId))
        {
            var worked = c.Pos.Ring(c.BorderLevel).Append(c.Pos).Where(h => State.Map.InBounds(h)).Select(h => State.Map.Tiles[h]).Take(6);
            var yields = worked.Select(Rules.Yield).OrderByDescending(y => y.food + y.prod + y.gold).Take(c.Pop).ToList();
            c.Food += yields.Sum(y => y.food) - c.Pop;
            c.Production += yields.Sum(y => y.prod);
            c.Science += Math.Max(1, yields.Sum(y => y.science) + 1);
            c.Culture += 2;
            State.Players[playerId].Gold += yields.Sum(y => y.gold);
            State.Players[playerId].CultureTotal += c.Culture;
            if (c.Food >= 10 + c.Pop * 4) { c.Food = 0; c.Pop++; }
            if (c.Culture >= 10 + c.BorderLevel * 8) { c.Culture = 0; c.BorderLevel++; ExpandBorders(c); }
            if (c.QueueItem is not null && c.Production >= 20)
            {
                c.Production = 0;
                if (Enum.TryParse<UnitType>(c.QueueItem, out var ut) && CanProduce(playerId, ut))
                    State.Units[_nextUnitId] = new Unit { Id = _nextUnitId++, OwnerId = c.OwnerId, Type = ut, Pos = c.Pos, MovePoints = Rules.BaseMove(ut) };
            }
        }

        var p = State.Players[playerId];
        if (p.ActiveTech is not null)
        {
            var sci = State.Cities.Values.Where(c => c.OwnerId == playerId).Sum(c => c.Science) + 2;
            sci = ModifierSystem.Apply(State, playerId, "CityYields", "Science", sci);
            p.ResearchProgress += sci;
            if (p.ResearchProgress >= 25) { p.UnlockedTechs.Add(p.ActiveTech.Value); p.ResearchProgress = 0; }
        }
    }

    private void ExpandBorders(City city)
    {
        foreach (var h in city.Pos.Ring(city.BorderLevel))
            if (State.Map.InBounds(h)) State.Map.Tiles[h].OwnerId = city.OwnerId;
    }

    private void HealUnits(int playerId)
    {
        foreach (var u in State.Units.Values.Where(u => u.OwnerId == playerId))
        {
            var friendly = State.Map.Tiles.TryGetValue(u.Pos, out var tile) && tile.OwnerId == playerId;
            var heal = u.Fortified ? (friendly ? 20 : 12) : (friendly ? 12 : 6);
            u.Hp = Math.Min(100, u.Hp + heal);
        }
    }

    private void UpdateFog(int playerId)
    {
        var p = State.Players[playerId];
        p.Visible.Clear();
        foreach (var u in State.Units.Values.Where(x => x.OwnerId == playerId))
        {
            var r = Rules.VisibilityRange(State.Map.Tiles[u.Pos], u.Type);
            foreach (var t in u.Pos.Ring(r).Append(u.Pos))
            {
                if (!State.Map.InBounds(t)) continue;
                if (BlocksLos(u.Pos, t)) continue;
                p.Visible.Add(t);
                p.Explored.Add(t);
            }
        }
    }

    private bool BlocksLos(Hex from, Hex to)
    {
        foreach (var step in Hex.Line(from, to).Skip(1).SkipLast(1))
            if (State.Map.InBounds(step) && State.Map.Tiles[step].Tile.Terrain == TerrainType.Mountain) return true;
        return false;
    }

    private void RunAiIfNeeded()
    {
        while (State.Players[State.ActivePlayerId].IsAi)
        {
            var ai = new AiController();
            foreach (var command in ai.GenerateTurnCommands(State, State.ActivePlayerId)) Apply(command);
            Apply(new EndTurnCommand(State.ActivePlayerId));
            if (!State.Players[State.ActivePlayerId].IsAi) break;
        }
    }

    private bool CanProduce(int playerId, UnitType ut)
    {
        if (ut != UnitType.Swordsman) return true;
        return State.Map.Tiles.Values.Any(t => t.OwnerId == playerId && t.Resource == ResourceType.Iron && t.ResourceActive);
    }

    public void SpawnInitialUnits(int playerId, Hex start)
    {
        State.Units[_nextUnitId] = new Unit { Id = _nextUnitId++, OwnerId = playerId, Type = UnitType.Settler, Pos = start, MovePoints = 2 };
        State.Units[_nextUnitId] = new Unit { Id = _nextUnitId++, OwnerId = playerId, Type = UnitType.Warrior, Pos = new Hex(start.Q + 1, start.R), MovePoints = 2 };
        State.Units[_nextUnitId] = new Unit { Id = _nextUnitId++, OwnerId = playerId, Type = UnitType.Builder, Pos = new Hex(start.Q, start.R + 1), MovePoints = 2 };
    }

    private DiplomacyRelation Relation(int a, int b)
    {
        var (x, y) = a < b ? (a, b) : (b, a);
        return State.Diplomacy.FirstOrDefault(d => d.A == x && d.B == y) ?? new DiplomacyRelation(x, y, DiplomacyState.War, false);
    }

    private void UpsertRelation(int a, int b, DiplomacyState state, bool openBorders)
    {
        var (x, y) = a < b ? (a, b) : (b, a);
        State.Diplomacy.RemoveAll(d => d.A == x && d.B == y);
        State.Diplomacy.Add(new DiplomacyRelation(x, y, state, openBorders));
    }

    private VictoryType EvaluateVictory()
    {
        var livingCapitals = State.Cities.Values.Where(c => c.IsCapital).Select(c => c.OwnerId).Distinct().ToList();
        if (livingCapitals.Count == 1) return VictoryType.Domination;
        if (State.Players.Values.Any(p => p.UnlockedTechs.Contains(TechType.Astronomy))) return VictoryType.Science;
        if (State.Players.Values.Any(p => p.CultureTotal >= 200)) return VictoryType.Culture;
        return VictoryType.None;
    }
}
