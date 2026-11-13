using Silk.NET.OpenGL;

namespace Visualizations.App.Rendering;

/// <summary>
/// Vykreslí jeden trojúhelník přes celé plátno; pozice se počítají ve vertex shaderu z gl_VertexID.
/// Základ pro všechny efekty, které jsou celé ve fragment shaderu.
/// </summary>
internal sealed class FullscreenPass : IDisposable
{
    /// <summary>Vertex shader, který do fragment shaderu předá <c>vUv</c> (0–1 přes celé plátno).</summary>
    public const string VertexShader = """
        #version 330 core
        out vec2 vUv;
        void main()
        {
            vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
            vUv = p;
            gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly uint _vao;

    public FullscreenPass(GL gl)
    {
        _gl = gl;
        _vao = gl.GenVertexArray();
    }

    public void Draw()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
    }

    public void Dispose() => _gl.DeleteVertexArray(_vao);
}
