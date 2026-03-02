using Engine.Platform;
using Silk.NET.Input;
using System.Numerics;

namespace Engine.Rendering;

public sealed class Camera3D
{
    public Vector3 Position { get; private set; } = new(0, 8, 12);
    public float Yaw { get; private set; } = -1.57f;
    public float Pitch { get; private set; } = -0.5f;
    public float Zoom { get; private set; } = 1.0f;

    private Vector2 _lastMouse;

    public void Update(InputState input, float dt)
    {
        var move = Vector3.Zero;
        if (input.IsKeyDown(Key.W)) move.Z -= 1;
        if (input.IsKeyDown(Key.S)) move.Z += 1;
        if (input.IsKeyDown(Key.A)) move.X -= 1;
        if (input.IsKeyDown(Key.D)) move.X += 1;
        var speed = 8f * dt;
        Position += Vector3.TransformNormal(move * speed, Matrix4x4.CreateRotationY(Yaw));

        if (input.RightDown)
        {
            var delta = input.MousePosition - _lastMouse;
            Yaw -= delta.X * 0.01f;
            Pitch = System.Math.Clamp(Pitch - delta.Y * 0.01f, -1.2f, 0.2f);
        }

        Zoom = System.Math.Clamp(Zoom - input.MouseWheelDelta * 0.05f, 0.4f, 2f);
        _lastMouse = input.MousePosition;
    }

    public Matrix4x4 View()
    {
        var dir = new Vector3(MathF.Cos(Pitch) * MathF.Cos(Yaw), MathF.Sin(Pitch), MathF.Cos(Pitch) * MathF.Sin(Yaw));
        return Matrix4x4.CreateLookAt(Position, Position + dir, Vector3.UnitY);
    }

    public Matrix4x4 Projection(float aspect) => Matrix4x4.CreatePerspectiveFieldOfView(0.9f / Zoom, aspect, 0.1f, 500f);
}
