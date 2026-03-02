using Silk.NET.OpenGL;
using System.Numerics;

namespace Engine.Graphics;

public sealed class ShaderProgram : IDisposable
{
    public uint Handle { get; }
    private readonly GL _gl;

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
        var v = Compile(ShaderType.VertexShader, vertexSource);
        var f = Compile(ShaderType.FragmentShader, fragmentSource);
        Handle = _gl.CreateProgram();
        _gl.AttachShader(Handle, v);
        _gl.AttachShader(Handle, f);
        _gl.LinkProgram(Handle);
        _gl.GetProgram(Handle, GLEnum.LinkStatus, out var ok);
        if (ok == 0) throw new Exception($"Program link error: {_gl.GetProgramInfoLog(Handle)}");
        _gl.DeleteShader(v);
        _gl.DeleteShader(f);
    }

    private uint Compile(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var ok);
        if (ok == 0) throw new Exception($"Shader compile error ({type}): {_gl.GetShaderInfoLog(shader)}");
        return shader;
    }

    public void Use() => _gl.UseProgram(Handle);
    public unsafe void SetMatrix4(string name, Matrix4x4 value)
    {
        var loc = _gl.GetUniformLocation(Handle, name);
        _gl.UniformMatrix4(loc, 1, false, (float*)&value);
    }

    public void SetVector3(string name, Vector3 value)
    {
        var loc = _gl.GetUniformLocation(Handle, name);
        _gl.Uniform3(loc, value.X, value.Y, value.Z);
    }

    public void SetFloat(string name, float value)
    {
        var loc = _gl.GetUniformLocation(Handle, name);
        _gl.Uniform1(loc, value);
    }

    public void Dispose() => _gl.DeleteProgram(Handle);
}
