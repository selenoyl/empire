using Engine.Diagnostics;
using Engine.Platform;

namespace Engine.Core;

public abstract class GameApp
{
    protected readonly EngineLogger Logger;
    protected readonly EngineConfig Config;
    protected GameWindowHost? WindowHost;

    protected GameApp(EngineConfig config, EngineLogger logger)
    {
        Config = config;
        Logger = logger;
    }

    public void Run()
    {
        WindowHost = new GameWindowHost(Config, Logger);
        WindowHost.OnLoad += HandleLoad;
        WindowHost.OnUpdate += HandleUpdate;
        WindowHost.OnRender += HandleRender;
        WindowHost.OnResize += OnResize;
        WindowHost.OnShutdown += OnShutdown;

        try { WindowHost.Run(); }
        catch (Exception ex)
        {
            Logger.Error("Fatal exception", ex);
            WindowHost?.SetErrorOverlay($"A fatal error occurred: {ex.Message}");
        }
    }

    protected abstract void OnLoad();
    protected abstract void OnUpdate(double dt);
    protected abstract void OnRender(double dt);
    protected virtual void OnResize(int width, int height) { }
    protected virtual void OnShutdown() { }

    private void HandleLoad() => OnLoad();
    private void HandleUpdate(double dt) => OnUpdate(dt);
    private void HandleRender(double dt) => OnRender(dt);
}
