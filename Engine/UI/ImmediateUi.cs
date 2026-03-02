using Engine.Platform;
using Silk.NET.Input;

namespace Engine.UI;

public readonly record struct UiRect(int X, int Y, int W, int H)
{
    public bool Contains(float px, float py) => px >= X && py >= Y && px <= X + W && py <= Y + H;
}

public sealed class ImmediateUi
{
    private readonly List<string> _textLines = [];
    private bool _clickConsumed;

    public void BeginFrame() { _textLines.Clear(); _clickConsumed = false; }
    public void Panel(string title) => _textLines.Add($"[{title}]");
    public void Label(string text) => _textLines.Add(text);

    public bool Button(string label, UiRect rect, InputState input)
    {
        _textLines.Add($"[Button] {label}");
        if (_clickConsumed) return false;
        if (input.IsMouseDown(MouseButton.Left) && rect.Contains(input.MousePosition.X, input.MousePosition.Y))
        {
            _clickConsumed = true;
            return true;
        }

        return false;
    }

    public string OverlayText => string.Join(" | ", _textLines);
}
