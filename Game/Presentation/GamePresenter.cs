using Engine.Graphics;
using Engine.Math;
using Engine.Rendering;
using Engine.UI;
using Game.CoreSim;
using Game.Net;
using Game.Diagnostics;
using Silk.NET.Input;
using System.Numerics;

namespace Game.Presentation;

public sealed class GamePresenter
{
    private readonly Simulator _sim;
    private readonly GameClient? _client;
    private readonly Camera3D _camera = new();
    private readonly ImmediateUi _ui = new();
    private readonly Dictionary<string, Material> _mats = [];
    private readonly List<RenderItem> _items = [];
    private readonly Queue<string> _eventPanel = new();
    private ForwardRenderer? _renderer;
    private Silk.NET.OpenGL.GL? _gl;
    private int _selectedUnitId;
    private readonly bool _replayWatermark;
    private readonly bool _devTools;

    public GamePresenter(Simulator sim, GameClient? client = null, bool devTools = false) { _sim = sim; _client = client; _replayWatermark = false; _devTools = devTools; }

    public void Load(Silk.NET.OpenGL.GL gl, string contentDir, bool connectedMode)
    {
        _gl = gl;
        if (_client is not null)
        {
            _client.EventReceived += evt => PushEvent($"NET: {evt.Message} {(evt.HashMismatch ? "[DESYNC]" : "")}");
        }

        var shader = new ShaderProgram(gl, File.ReadAllText(Path.Combine(contentDir, "Shaders/lit.vert")), File.ReadAllText(Path.Combine(contentDir, "Shaders/lit.frag")));
        _mats["Grass"] = new Material(shader) { Color = new Vector3(0.35f, 0.72f, 0.35f) };
        _mats["Plains"] = new Material(shader) { Color = new Vector3(0.67f, 0.69f, 0.4f) };
        _mats["Desert"] = new Material(shader) { Color = new Vector3(0.82f, 0.75f, 0.5f) };
        _mats["Hills"] = new Material(shader) { Color = new Vector3(0.5f, 0.45f, 0.35f) };
        _mats["Water"] = new Material(shader) { Color = new Vector3(0.2f, 0.35f, 0.8f) };
        _mats["Mountain"] = new Material(shader) { Color = new Vector3(0.55f, 0.55f, 0.55f) };
        _mats["Unit"] = new Material(shader) { Color = new Vector3(0.95f, 0.2f, 0.2f) };
        _mats["City"] = new Material(shader) { Color = new Vector3(0.85f, 0.85f, 0.9f) };

        _renderer = new ForwardRenderer(gl);
        _renderer.Initialize();
        RebuildScene(gl);
    }

    public void Update(Engine.Platform.InputState input, double dt)
    {
        _camera.Update(input, (float)dt);
        if (input.IsMouseDown(MouseButton.Left)) SelectClosest();

        if (_devTools && input.IsKeyDown(Key.F5))
        {
            var dump = Path.Combine("logs", $"state_dump_t{_sim.State.Turn}.json");
            Directory.CreateDirectory("logs");
            File.WriteAllText(dump, System.Text.Json.JsonSerializer.Serialize(SnapshotMapper.ToDto(_sim.State), new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            PushEvent($"State dumped: {dump}");
        }
        if (_devTools && input.IsKeyDown(Key.F6))
        {
            SaveLoad.Save("savegame.json", _sim);
            PushEvent("Saved savegame.json");
        }

        if (_devTools && input.IsKeyDown(Key.F7) && _client is not null)
        {
            _client.RequestSnapshotAsync(_sim.State.ActivePlayerId, "manual-desync-recover", CancellationToken.None).GetAwaiter().GetResult();
            PushEvent("Requested snapshot resend");
        }

        if (input.IsMouseDown(MouseButton.Right) && _selectedUnitId != 0)
        {
            if (!_sim.State.Units.TryGetValue(_selectedUnitId, out var u))
            {
                _selectedUnitId = 0;
                return;
            }

            var t = new Hex(Math.Clamp((int)_camera.Position.X + 20, 0, _sim.State.Map.Width - 1), Math.Clamp((int)_camera.Position.Z + 12, 0, _sim.State.Map.Height - 1));
            var r = _sim.Apply(new MoveUnitCommand(_sim.State.ActivePlayerId, u.Id, t));
            PushEvent(r.Message);
            DebugTrace.Record("input", $"RClick move unit={u.Id} target={t.Q},{t.R} result={r.Message}");
        }
    }

    public void Render(int w, int h, Engine.Platform.InputState input)
    {
        _renderer!.Render(_items, _camera, w, h, Vector3.Normalize(new(-0.7f, -1, -0.4f)));
        _ui.BeginFrame();
        _ui.Panel("Top Bar");
        _ui.Label($"Turn {_sim.State.Turn}/{_sim.State.Config.MaxTurns} | Active P{_sim.State.ActivePlayerId}");
        if (_devTools)
        {
            _ui.Label($"Hash {_sim.State.ComputeStateHash()[..8]}");
            if (_client is not null) _ui.Label($"ServerHash {_client.LastServerHash[..Math.Min(8, _client.LastServerHash.Length)]} {_client.HashMismatch}");
        }
        if (_ui.Button("End Turn", new UiRect(10, 12, 120, 22), input)) PushEvent(_sim.Apply(new EndTurnCommand(_sim.State.ActivePlayerId)).Message);
        if (_ui.Button("Found City", new UiRect(10, 38, 120, 22), input) && _selectedUnitId != 0) PushEvent(_sim.Apply(new FoundCityCommand(_sim.State.ActivePlayerId, _selectedUnitId, "New City")).Message);
        if (_ui.Button("Set Research", new UiRect(10, 64, 120, 22), input)) PushEvent(_sim.Apply(new SetResearchCommand(_sim.State.ActivePlayerId, TechType.Archery)).Message);
        if (_ui.Button("Queue Archer", new UiRect(10, 90, 120, 22), input))
        {
            var city = _sim.State.Cities.Values.FirstOrDefault(c => c.OwnerId == _sim.State.ActivePlayerId);
            if (city is not null) PushEvent(_sim.Apply(new SetCityProductionCommand(_sim.State.ActivePlayerId, city.Id, UnitType.Archer.ToString())).Message);
        }
        if (_ui.Button("Build Farm", new UiRect(10, 116, 120, 22), input) && _selectedUnitId != 0) PushEvent(_sim.Apply(new BuildImprovementCommand(_sim.State.ActivePlayerId, _selectedUnitId, ImprovementType.Farm)).Message);

        if (_devTools)
        {
            _ui.Panel("Event Log");
            foreach (var e in _eventPanel.Reverse().Take(5)) _ui.Label(e);
            foreach (var d in DebugTrace.SnapshotRecent(4)) _ui.Label(d);
            if (_replayWatermark) _ui.Label("REPLAY MODE");
        }
        Console.Title = _ui.OverlayText;
        RefreshDynamicItems(_gl);
    }

    private void PushEvent(string msg)
    {
        DebugTrace.Record("event", $"T{_sim.State.Turn}: {msg}");
        _eventPanel.Enqueue($"T{_sim.State.Turn}: {msg}");
        while (_eventPanel.Count > 30) _eventPanel.Dequeue();
    }

    private void RebuildScene(Silk.NET.OpenGL.GL gl)
    {
        var hex = ProceduralMeshes.HexPrism(0.48f, 0.2f);
        foreach (var t in _sim.State.Map.Tiles.Values)
        {
            var pos = HexToWorld(t.Tile.Hex);
            _items.Add(new RenderItem
            {
                Id = _items.Count + 1,
                Mesh = new MeshBuffer(gl, hex.Item1, hex.Item2),
                Material = _mats[t.Tile.Terrain.ToString()],
                Transform = Matrix4x4.CreateTranslation(pos),
                Bounds = new Aabb(pos - Vector3.One, pos + Vector3.One)
            });
        }
        RefreshDynamicItems(gl);
    }

    private void RefreshDynamicItems(Silk.NET.OpenGL.GL? gl = null)
    {
        if (gl is null) return;
        _items.RemoveAll(i => i.Id >= 100000);
        var marker = ProceduralMeshes.Marker();
        foreach (var u in _sim.State.Units.Values)
        {
            var pos = HexToWorld(u.Pos) + new Vector3(0, 0.35f, 0);
            _items.Add(new RenderItem { Id = 100000 + u.Id, Mesh = new MeshBuffer(gl, marker.Item1, marker.Item2), Material = _mats["Unit"], Transform = Matrix4x4.CreateScale(1.3f, 1, 1.3f) * Matrix4x4.CreateTranslation(pos), Bounds = new Aabb(pos - Vector3.One, pos + Vector3.One), Selected = u.Id == _selectedUnitId });
        }
        var cube = ProceduralMeshes.Cube(0.5f);
        foreach (var c in _sim.State.Cities.Values)
        {
            var pos = HexToWorld(c.Pos) + new Vector3(0, 0.45f, 0);
            _items.Add(new RenderItem { Id = 200000 + c.Id, Mesh = new MeshBuffer(gl, cube.Item1, cube.Item2), Material = _mats["City"], Transform = Matrix4x4.CreateTranslation(pos), Bounds = new Aabb(pos - Vector3.One, pos + Vector3.One) });
        }
    }

    private void SelectClosest()
    {
        var u = _sim.State.Units.Values.Where(u => u.OwnerId == _sim.State.ActivePlayerId).OrderBy(u => u.Pos.Distance(new Hex((int)_camera.Position.X + 20, (int)_camera.Position.Z + 12))).FirstOrDefault();
        _selectedUnitId = u?.Id ?? 0;
    }

    private static Vector3 HexToWorld(Hex h)
    {
        var x = h.Q + h.R * 0.5f;
        var z = h.R * 866 / 1000f;
        return new Vector3(x - 20, 0, z - 10);
    }
}
