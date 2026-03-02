namespace Engine.Core;

public sealed class EngineConfig
{
    public string Title { get; init; } = "Empire Sandbox";
    public int Width { get; init; } = 1280;
    public int Height { get; init; } = 720;
    public bool VSync { get; set; } = true;
}
