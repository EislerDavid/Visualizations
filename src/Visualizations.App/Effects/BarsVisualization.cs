using Silk.NET.OpenGL;
using Visualizations.App.Rendering;
using Visualizations.Core.Analysis;

namespace Visualizations.App.Effects;

/// <summary>
/// „Bars“ z rodiny Bars and Waves: spektrum jako sloupce s barevným přechodem podle výšky
/// a padajícími špičkami.
/// </summary>
internal sealed unsafe class BarsVisualization : IVisualization
{
    // Vrchol: pozice (x, y) v NDC, úroveň 0–1 (výška v okně, určuje barvu), 1 = špička / 0 = sloupec.
    private const int FloatsPerVertex = 4;
    private const int VerticesPerQuad = 6;

    private const float GapFraction = 0.2f;
    private const float PeakThicknessPixels = 3f;

    private const string VertexShader = """
        #version 330 core
        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in float aLevel;
        layout (location = 2) in float aPeak;
        out float vLevel;
        out float vPeak;
        void main()
        {
            vLevel = aLevel;
            vPeak = aPeak;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
        """;

    private const string FragmentShader = """
        #version 330 core
        in float vLevel;
        in float vPeak;
        out vec4 fragColor;
        void main()
        {
            vec3 low  = vec3(0.00, 0.80, 0.25);
            vec3 mid  = vec3(1.00, 0.90, 0.10);
            vec3 high = vec3(1.00, 0.15, 0.10);
            vec3 bar = vLevel < 0.6
                ? mix(low, mid, vLevel / 0.6)
                : mix(mid, high, (vLevel - 0.6) / 0.4);
            fragColor = vec4(mix(bar, vec3(1.0), vPeak * 0.85), 1.0);
        }
        """;

    private GL? _gl;
    private ShaderProgram? _program;
    private uint _vao;
    private uint _vbo;
    private float[] _vertices = [];
    private int _height = 1;

    public string Name => "Bars";

    public void Load(GL gl)
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
        gl.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
    }

    public void Resize(int width, int height) => _height = Math.Max(height, 1);

    public void Render(SpectrumAnalyzer audio, float deltaTime)
    {
        if (_gl is null || _program is null)
        {
            return;
        }

        var bands = audio.Bands;
        var peaks = audio.Peaks;
        int quadCount = bands.Length * 2;
        int floatCount = quadCount * VerticesPerQuad * FloatsPerVertex;
        if (_vertices.Length != floatCount)
        {
            _vertices = new float[floatCount];
        }

        float slot = 2f / bands.Length;
        float barWidth = slot * (1f - GapFraction);
        float peakThickness = PeakThicknessPixels * 2f / _height;
        int offset = 0;

        for (int i = 0; i < bands.Length; i++)
        {
            float left = -1f + i * slot + (slot - barWidth) / 2f;
            float right = left + barWidth;

            float barTop = -1f + 2f * bands[i];
            offset = WriteQuad(_vertices, offset, left, right, -1f, barTop, 0f, bands[i], peak: 0f);

            float peakBottom = Math.Min(-1f + 2f * peaks[i], 1f - peakThickness);
            float peakLevel = (peakBottom + 1f) / 2f;
            offset = WriteQuad(_vertices, offset, left, right, peakBottom, peakBottom + peakThickness, peakLevel, peakLevel, peak: 1f);
        }

        _program.Use();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, _vertices, BufferUsageARB.StreamDraw);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(quadCount * VerticesPerQuad));
    }

    public void Dispose()
    {
        if (_gl is null)
        {
            return;
        }

        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _program?.Dispose();
    }

    private static int WriteQuad(
        float[] v, int o, float left, float right, float bottom, float top, float bottomLevel, float topLevel, float peak)
    {
        o = WriteVertex(v, o, left, bottom, bottomLevel, peak);
        o = WriteVertex(v, o, right, bottom, bottomLevel, peak);
        o = WriteVertex(v, o, right, top, topLevel, peak);
        o = WriteVertex(v, o, left, bottom, bottomLevel, peak);
        o = WriteVertex(v, o, right, top, topLevel, peak);
        return WriteVertex(v, o, left, top, topLevel, peak);
    }

    private static int WriteVertex(float[] v, int o, float x, float y, float level, float peak)
    {
        v[o] = x;
        v[o + 1] = y;
        v[o + 2] = level;
        v[o + 3] = peak;
        return o + FloatsPerVertex;
    }
}
