using Engine.Graphics;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Engine.Rendering;

public sealed class ForwardRenderer(GL gl)
{
    private readonly GL _gl = gl;
    public bool Wireframe { get; set; }

    public void Initialize()
    {
        _gl.Enable(GLEnum.DepthTest);
        _gl.Enable(GLEnum.CullFace);
    }

    public void Render(IReadOnlyList<RenderItem> items, Camera3D camera, int width, int height, Vector3 lightDir)
    {
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.ClearColor(0.12f, 0.15f, 0.2f, 1f);
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        _gl.PolygonMode(TriangleFace.FrontAndBack, Wireframe ? PolygonMode.Line : PolygonMode.Fill);

        foreach (var item in items)
        {
            var shader = item.Material.Shader;
            shader.Use();
            shader.SetMatrix4("uModel", item.Transform);
            shader.SetMatrix4("uView", camera.View());
            shader.SetMatrix4("uProj", camera.Projection(width / (float)height));
            shader.SetVector3("uColor", item.Selected ? new Vector3(1, 0.85f, 0.1f) : item.Material.Color);
            shader.SetVector3("uLightDir", lightDir);
            shader.SetFloat("uAmbient", 0.35f);
            item.Mesh.Draw();
        }
    }
}
