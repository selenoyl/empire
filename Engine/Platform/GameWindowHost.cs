using Engine.Core;
using Engine.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.Platform;

public sealed class GameWindowHost
{
    private readonly IWindow _window;
    private readonly EngineConfig _config;
    private readonly EngineLogger _logger;
    private IInputContext? _input;
    private string? _errorOverlay;

    public event Action? OnLoad;
    public event Action<double>? OnUpdate;
    public event Action<double>? OnRender;
    public event Action<int, int>? OnResize;
    public event Action? OnShutdown;

    public GL? Gl { get; private set; }
    public InputState Input { get; } = new();

    public GameWindowHost(EngineConfig config, EngineLogger logger)
    {
        _config = config;
        _logger = logger;
        var options = WindowOptions.Default with
        {
            Title = config.Title,
            Size = new Vector2D<int>(config.Width, config.Height),
            VSync = config.VSync,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 1))
        };

        _window = Window.Create(options);
        _window.Load += Load;
        _window.Update += dt => OnUpdate?.Invoke(dt);
        _window.Render += Render;
        _window.Resize += size => OnResize?.Invoke(size.X, size.Y);
        _window.Closing += () => OnShutdown?.Invoke();
    }

    private void Load()
    {
        Gl = GL.GetApi(_window);
        _input = _window.CreateInput();
        Input.Attach(_input);
        _window.VSync = _config.VSync;
        OnLoad?.Invoke();
    }

    private void Render(double dt)
    {
        try { OnRender?.Invoke(dt); }
        catch (Exception ex)
        {
            _logger.Error("Render exception", ex);
            _errorOverlay = ex.Message;
        }
    }

    public void SetVSync(bool enabled)
    {
        _config.VSync = enabled;
        _window.VSync = enabled;
    }

    public void RequestClose() => _window.Close();
    public void SetErrorOverlay(string message) => _errorOverlay = message;
    public string? ErrorOverlay => _errorOverlay;
    public (int Width, int Height) Size => (_window.Size.X, _window.Size.Y);
    public void Run() => _window.Run();
}
