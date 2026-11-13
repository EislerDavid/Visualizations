using System.Numerics;
using Silk.NET.OpenGL;

namespace Visualizations.App.Rendering;

/// <summary>
/// Zářící čára (otevřená nebo uzavřená) přes body v pixelech: široká slabá záře + úzké jasné jádro,
/// míchané aditivně do aktuálně nastaveného plátna.
/// </summary>
internal sealed unsafe class GlowLine : IDisposable
{
    // Vrchol: střed čáry (x, y v pixelech), normála (x, y), strana −1 / +1.
    private const int FloatsPerVertex = 5;

    private const string VertexShader = """
        #version 330 core
        layout (location = 0) in vec2 aCenter;
        layout (location = 1) in vec2 aNormal;
        layout (location = 2) in float aSide;
        uniform vec2 uResolution;
        uniform float uHalfWidth;
        out float vSide;
        void main()
        {
            vSide = aSide;
            vec2 p = aCenter + aNormal * aSide * uHalfWidth;
            gl_Position = vec4(p / uResolution * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    private const string FragmentShader = """
        #version 330 core
        in float vSide;
        out vec4 fragColor;
        uniform vec3 uColor;
        uniform float uIntensity;
        uniform float uSharpness;
        void main()
        {
            float profile = pow(1.0 - abs(vSide), uSharpness);
            fragColor = vec4(uColor * profile * uIntensity, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly ShaderProgram _program;
    private readonly uint _vao;
    private readonly uint _vbo;
    private float[] _vertices = [];

    public GlowLine(GL gl)
    {
        _gl = gl;
        _program = new ShaderProgram(gl, VertexShader, FragmentShader);
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        uint stride = FloatsPerVertex * sizeof(float);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, (void*)(4 * sizeof(float)));
    }

    /// <param name="points">Body čáry v pixelech (počátek vlevo dole).</param>
    /// <param name="closed">Spojit poslední bod s prvním.</param>
    /// <param name="resolution">Velikost plátna v pixelech.</param>
    public void Draw(
        ReadOnlySpan<Vector2> points,
        bool closed,
        Vector3 color,
        Vector2 resolution,
        float coreHalfWidth = 1.8f,
        float glowHalfWidth = 9f,
        float coreIntensity = 1f,
        float glowIntensity = 0.35f)
    {
        int n = points.Length;
        if (n < 2)
        {
            return;
        }

        int count = closed ? n + 1 : n;
        int floatCount = count * 2 * FloatsPerVertex;
        if (_vertices.Length < floatCount)
        {
            _vertices = new float[floatCount];
        }

        int o = 0;
        for (int i = 0; i < count; i++)
        {
            int index = i % n;
            Vector2 previous = closed ? points[(index - 1 + n) % n] : points[Math.Max(index - 1, 0)];
            Vector2 next = closed ? points[(index + 1) % n] : points[Math.Min(index + 1, n - 1)];
            Vector2 tangent = next - previous;
            Vector2 normal = tangent.LengthSquared() > 1e-8f
                ? Vector2.Normalize(new Vector2(-tangent.Y, tangent.X))
                : Vector2.UnitY;
            o = WriteVertex(o, points[index], normal, -1f);
            o = WriteVertex(o, points[index], normal, 1f);
        }

        _program.Use();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, _vertices.AsSpan(0, floatCount), BufferUsageARB.StreamDraw);
        _program.Set("uResolution", resolution);
        _program.Set("uColor", color);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
        DrawPass(count, glowHalfWidth, glowIntensity, sharpness: 2f);
        DrawPass(count, coreHalfWidth, coreIntensity, sharpness: 0.5f);
        _gl.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _program.Dispose();
    }

    private void DrawPass(int count, float halfWidth, float intensity, float sharpness)
    {
        _program.Set("uHalfWidth", halfWidth);
        _program.Set("uIntensity", intensity);
        _program.Set("uSharpness", sharpness);
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, (uint)(count * 2));
    }

    private int WriteVertex(int o, Vector2 center, Vector2 normal, float side)
    {
        _vertices[o] = center.X;
        _vertices[o + 1] = center.Y;
        _vertices[o + 2] = normal.X;
        _vertices[o + 3] = normal.Y;
        _vertices[o + 4] = side;
        return o + FloatsPerVertex;
    }
}
