using Engine.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Graphics;

public sealed class MeshBuffer : IDisposable
{
    private readonly GL _gl;
    public uint Vao { get; }
    private readonly uint _vbo;
    private readonly uint _ebo;
    public int IndexCount { get; }

    public unsafe MeshBuffer(GL gl, IReadOnlyList<Vertex> vertices, IReadOnlyList<uint> indices)
    {
        _gl = gl;
        IndexCount = indices.Count;
        Vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(Vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (Vertex* ptr = vertices.ToArray())
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Count * sizeof(Vertex)), ptr, BufferUsageARB.StaticDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* iptr = indices.ToArray())
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Count * sizeof(uint)), iptr, BufferUsageARB.StaticDraw);

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)(sizeof(float) * 3));
    }

    public unsafe void Draw()
    {
        _gl.BindVertexArray(Vao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)IndexCount, DrawElementsType.UnsignedInt, null);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(Vao);
    }
}
