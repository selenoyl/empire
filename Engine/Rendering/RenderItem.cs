using Engine.Math;
using Engine.Graphics;
using System.Numerics;

namespace Engine.Rendering;

public sealed class RenderItem
{
    public int Id { get; init; }
    public required MeshBuffer Mesh { get; init; }
    public required Material Material { get; init; }
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
    public Aabb Bounds { get; set; }
    public bool Selected { get; set; }
}
