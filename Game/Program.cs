using Engine.Core;
using Engine.Diagnostics;
using Game.CoreSim;
using Game.Net;
using Game.Presentation;
using System.Text.Json;
using Game.Diagnostics;

var devTools = args.Contains("--devtools") || Environment.GetEnvironmentVariable("EMPIRE_DEVTOOLS") == "1";
DebugTrace.Initialize(devTools);

var logger = new EngineLogger("EmpireEngine", "1.0.0", devTools);
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is Exception ex)
    {
        logger.Error("Unhandled exception", ex);
        DebugTrace.RecordException("fatal", ex);
    }
};
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    logger.Error("Unobserved task exception", e.Exception);
    DebugTrace.RecordException("task", e.Exception);
    e.SetObserved();
};

var positionalArgs = args.Where(a => !a.StartsWith("--")).ToArray();
var mode = positionalArgs.FirstOrDefault() ?? "single";
if (mode == "host")
{
    var sim = Bootstrap.CreateSim(seed: 1234, mapSize: "Small", playerCount: 2, aiPlayers: 1, maxTurns: 200);
    var server = new GameServer(7777, sim, "1.0.0");
    Console.WriteLine("Hosting on 7777...");
    await server.RunAsync(CancellationToken.None);
    return;
}

if (mode == "replay")
{
    var file = positionalArgs.Skip(1).FirstOrDefault() ?? "replay.json";
    if (!File.Exists(file)) throw new FileNotFoundException(file);
    var replay = JsonSerializer.Deserialize<ReplayFile>(File.ReadAllText(file))!;
    var sim = Bootstrap.FromSnapshot(replay.InitialSnapshot);
    foreach (var cmd in ReplayDeserializer.ParseCommands(replay.Commands)) sim.Apply(cmd);
    Console.WriteLine($"Replay hash: {sim.State.ComputeStateHash()}");
    return;
}

var app = new CivSliceApp(new EngineConfig { Title = "Empire 4X v1.0" }, logger, mode, positionalArgs.Skip(1).ToArray(), devTools);
app.Run();

sealed class CivSliceApp(EngineConfig config, EngineLogger logger, string mode, string[] rest, bool devTools) : GameApp(config, logger)
{
    private Simulator? _sim;
    private GamePresenter? _presenter;
    private GameClient? _client;

    protected override void OnLoad()
    {
        if (mode == "join")
        {
            var host = rest.FirstOrDefault() ?? "127.0.0.1:7777";
            var hp = host.Split(':');
            _client = new GameClient();
            _client.ConnectAsync(hp[0], int.Parse(hp[1]), "Client", false, CancellationToken.None).GetAwaiter().GetResult();
            var snap = _client.Snapshot ?? throw new InvalidOperationException("No snapshot");
            _sim = Bootstrap.FromSnapshot(snap);
        }
        else if (mode == "load")
        {
            var path = rest.FirstOrDefault() ?? "savegame.json";
            if (!SaveLoad.TryLoad(path, out var file, out var err)) throw new InvalidDataException(err);
            _sim = Bootstrap.FromSnapshot(file!.Snapshot);
        }
        else
        {
            _sim = Bootstrap.CreateSim(seed: 1234, mapSize: "Small", playerCount: 2, aiPlayers: 1, maxTurns: 200);
        }

        _presenter = new GamePresenter(_sim, _client, devTools);
        _presenter.Load(WindowHost!.Gl!, Path.Combine(AppContext.BaseDirectory, "Content"), mode == "join");
    }

    protected override void OnUpdate(double dt)
    {
        if (WindowHost!.Input.IsKeyDown(Silk.NET.Input.Key.Escape)) WindowHost.RequestClose();
        _presenter!.Update(WindowHost.Input, dt);
    }

    protected override void OnRender(double dt) => _presenter!.Render(WindowHost!.Size.Width, WindowHost.Size.Height, WindowHost.Input);
}

static class Bootstrap
{
    public static Simulator CreateSim(int seed, string mapSize, int playerCount, int aiPlayers, int maxTurns)
    {
        var map = MapGen.Create(mapSize, seed);
        var state = new SimState(seed, map) { Config = new MatchConfig { MapSize = mapSize, MaxTurns = maxTurns, ProtocolVersion = Protocol.Version, GameVersion = "1.0.0" } };
        foreach (var civ in LoadCivs()) state.Civs[civ.Id] = civ;
        var civs = state.Civs.Keys.OrderBy(x => x).ToList();
        for (var p = 1; p <= playerCount; p++)
            state.Players[p] = new PlayerState { PlayerId = p, CivId = civs[(p - 1) % civs.Count], IsAi = p <= aiPlayers };
        for (var p = 1; p <= playerCount; p++)
            for (var q = p + 1; q <= playerCount; q++) state.Diplomacy.Add(new DiplomacyRelation(p, q, DiplomacyState.War, false));

        var sim = new Simulator(state);
        sim.SpawnInitialUnits(1, new Hex(5, 5));
        sim.SpawnInitialUnits(2, new Hex(14, 10));
        return sim;
    }

    public static Simulator FromSnapshot(SnapshotDto snap)
    {
        var map = new MapData { Width = snap.Tiles.Max(t => t.Q) + 1, Height = snap.Tiles.Max(t => t.R) + 1 };
        foreach (var t in snap.Tiles)
        {
            var h = new Hex(t.Q, t.R);
            map.Tiles[h] = new TileState { Tile = new Tile(h, Enum.Parse<TerrainType>(t.Terrain), t.Forest), OwnerId = t.OwnerId, Improvement = Enum.Parse<ImprovementType>(t.Improvement), Resource = Enum.Parse<ResourceType>(t.Resource), ResourceActive = t.ResourceActive };
        }
        var s = new SimState(snap.Seed, map) { Turn = snap.Turn, ActivePlayerId = snap.ActivePlayerId, Config = snap.Config };
        foreach (var p in snap.Players)
        {
            var ps = new PlayerState { PlayerId = p.PlayerId, CivId = p.CivId, IsAi = p.IsAi, Gold = p.Gold, ResearchProgress = p.ResearchProgress, CultureTotal = p.CultureTotal, ActiveTech = p.ActiveTech is null ? null : Enum.Parse<TechType>(p.ActiveTech) };
            foreach (var ut in p.UnlockedTechs) ps.UnlockedTechs.Add(Enum.Parse<TechType>(ut));
            s.Players[p.PlayerId] = ps;
        }
        foreach (var c in snap.Cities)
            s.Cities[c.Id] = new City { Id = c.Id, OwnerId = c.OwnerId, Name = c.Name, Pos = new Hex(c.Q, c.R), Pop = c.Pop, Food = c.Food, Production = c.Production, Science = c.Science, Culture = c.Culture, BorderLevel = c.BorderLevel, IsCapital = c.IsCapital, QueueItem = c.QueueItem };
        foreach (var u in snap.Units)
            s.Units[u.Id] = new Unit { Id = u.Id, OwnerId = u.OwnerId, Type = Enum.Parse<UnitType>(u.Type), Pos = new Hex(u.Q, u.R), Hp = u.Hp, MovePoints = Rules.BaseMove(Enum.Parse<UnitType>(u.Type)), Fortified = u.Fortified };
        foreach (var d in snap.Diplomacy)
            s.Diplomacy.Add(new DiplomacyRelation(d.A, d.B, Enum.Parse<DiplomacyState>(d.State), d.OpenBorders));
        foreach (var civ in LoadCivs()) s.Civs[civ.Id] = civ;
        return new Simulator(s);
    }

    private static List<CivDefinition> LoadCivs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Content", "Civs");
        return Directory.GetFiles(root, "*.json").Select(f => JsonSerializer.Deserialize<CivDefinition>(File.ReadAllText(f))!).ToList();
    }
}
