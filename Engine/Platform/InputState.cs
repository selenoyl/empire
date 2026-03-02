using Silk.NET.Input;
using System.Numerics;

namespace Engine.Platform;

public sealed class InputState
{
    private IKeyboard? _keyboard;
    private IMouse? _mouse;
    public Vector2 MousePosition { get; private set; }
    public float MouseWheelDelta { get; private set; }
    public bool LeftDown => _mouse?.IsButtonPressed(MouseButton.Left) ?? false;
    public bool RightDown => _mouse?.IsButtonPressed(MouseButton.Right) ?? false;

    public void Attach(IInputContext input)
    {
        _keyboard = input.Keyboards.FirstOrDefault();
        _mouse = input.Mice.FirstOrDefault();
        if (_mouse is not null)
        {
            _mouse.MouseMove += (_, p) => MousePosition = p;
            _mouse.Scroll += (_, w) => MouseWheelDelta += w.Y;
        }
    }

    public bool IsKeyDown(Key key) => _keyboard?.IsKeyPressed(key) ?? false;
    public bool IsMouseDown(MouseButton button) => _mouse?.IsButtonPressed(button) ?? false;
    public void EndFrame() => MouseWheelDelta = 0;
}
