using Engine.Graphics;
using System.Numerics;

namespace Engine.Rendering;

public sealed class Material(ShaderProgram shader)
{
    public ShaderProgram Shader { get; } = shader;
    public Vector3 Color { get; set; } = new(0.8f);
}
