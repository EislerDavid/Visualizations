using System.Numerics;
using Silk.NET.OpenGL;

namespace Visualizations.App.Rendering;

/// <summary>Zkompilovaný a slinkovaný pár vertex + fragment shaderu.</summary>
internal sealed class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly Dictionary<string, int> _uniformLocations = [];

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
        uint vertex = Compile(ShaderType.VertexShader, vertexSource);
        uint fragment = Compile(ShaderType.FragmentShader, fragmentSource);

        Handle = gl.CreateProgram();
        gl.AttachShader(Handle, vertex);
        gl.AttachShader(Handle, fragment);
        gl.LinkProgram(Handle);
        gl.DetachShader(Handle, vertex);
        gl.DetachShader(Handle, fragment);
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);

        gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out int linked);
        if (linked == 0)
        {
            string log = gl.GetProgramInfoLog(Handle);
            gl.DeleteProgram(Handle);
            throw new InvalidOperationException($"Linkování shaderu selhalo: {log}");
        }
    }

    public uint Handle { get; }

    public void Use() => _gl.UseProgram(Handle);

    /// <summary>Poloha uniformu (s mezipamětí); −1, pokud ho shader nepoužívá – nastavení se pak ignoruje.</summary>
    public int GetUniformLocation(string name)
    {
        if (!_uniformLocations.TryGetValue(name, out int location))
        {
            location = _gl.GetUniformLocation(Handle, name);
            _uniformLocations[name] = location;
        }

        return location;
    }

    public void Set(string name, int value) => _gl.Uniform1(GetUniformLocation(name), value);

    public void Set(string name, float value) => _gl.Uniform1(GetUniformLocation(name), value);

    public void Set(string name, Vector2 value) => _gl.Uniform2(GetUniformLocation(name), value.X, value.Y);

    public void Set(string name, Vector3 value) => _gl.Uniform3(GetUniformLocation(name), value.X, value.Y, value.Z);

    public void Dispose() => _gl.DeleteProgram(Handle);

    private uint Compile(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compiled);
        if (compiled == 0)
        {
            string log = _gl.GetShaderInfoLog(shader);
            _gl.DeleteShader(shader);
            throw new InvalidOperationException($"Kompilace {type} selhala: {log}");
        }

        return shader;
    }
}
