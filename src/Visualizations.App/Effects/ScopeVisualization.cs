using System.Numerics;
using Silk.NET.OpenGL;
using Visualizations.App.Rendering;
using Visualizations.Core.Analysis;

namespace Visualizations.App.Effects;

/// <summary>
/// „Scope“ z rodiny Bars and Waves: osciloskop – průběh signálu jako zářící čára
/// s pomalu se měnící barvou. Spouštění (trigger) na průchodu nulou drží periodické tóny na místě.
/// </summary>
internal sealed unsafe class ScopeVisualization : IVisualization
{
    private const int PointCount = AudioFrame.WaveformLength;

    // Vrchol: střed čáry (x, y v pixelech), normála (x, y), strana −1 / +1.
    private const int FloatsPerVertex = 5;

    private const float AmplitudeScale = 2.5f;
    private const float HeightFraction = 0.42f;

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

    private readonly float[] _vertices = new float[PointCount * 2 * FloatsPerVertex];
    private readonly Vector2[] _points = new Vector2[PointCount];

    private GL? _gl;
    private ShaderProgram? _program;
    private uint _vao;
    private uint _vbo;
    private int _resolutionLocation;
    private int _halfWidthLocation;
    private int _colorLocation;
    private int _intensityLocation;
    private int _sharpnessLocation;

    public void Load(GL gl)
    {
        _gl = gl;
        _program = new ShaderProgram(gl, VertexShader, FragmentShader);
        _resolutionLocation = _program.GetUniformLocation("uResolution");
        _halfWidthLocation = _program.GetUniformLocation("uHalfWidth");
        _colorLocation = _program.GetUniformLocation("uColor");
        _intensityLocation = _program.GetUniformLocation("uIntensity");
        _sharpnessLocation = _program.GetUniformLocation("uSharpness");

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

    public void Resize(int width, int height)
    {
    }

    public void Render(in FrameContext frame)
    {
        if (_gl is null || _program is null)
        {
            return;
        }

        float width = frame.Target.Width;
        float height = frame.Target.Height;
        var samples = frame.Audio.RecentSamples;
        int start = FindTrigger(samples);
        float gain = AmplitudeScale * frame.Audio.Gain;

        for (int i = 0; i < PointCount; i++)
        {
            float value = Math.Clamp(samples[start + i] * gain, -1f, 1f);
            _points[i] = new Vector2(i * width / (PointCount - 1), height * (0.5f + HeightFraction * value));
        }

        int o = 0;
        for (int i = 0; i < PointCount; i++)
        {
            var tangent = _points[Math.Min(i + 1, PointCount - 1)] - _points[Math.Max(i - 1, 0)];
            var normal = Vector2.Normalize(new Vector2(-tangent.Y, tangent.X));
            o = WriteVertex(o, _points[i], normal, -1f);
            o = WriteVertex(o, _points[i], normal, 1f);
        }

        _program.Use();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, _vertices, BufferUsageARB.StreamDraw);
        _gl.Uniform2(_resolutionLocation, width, height);

        var color = Hsv((float)(frame.Time * 0.03 % 1.0), 0.7f, 1f);
        _gl.Uniform3(_colorLocation, color.X, color.Y, color.Z);

        // Aditivní míchání: široká slabá záře + úzké jasné jádro.
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
        DrawPass(halfWidth: 9f, intensity: 0.35f, sharpness: 2f);
        DrawPass(halfWidth: 1.8f, intensity: 1f, sharpness: 0.5f);
        _gl.Disable(EnableCap.Blend);
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

    /// <summary>
    /// Najde nejnovější vzestupný průchod nulou, za kterým je ještě celé okno vzorků;
    /// když žádný není, vezme prostě posledních <see cref="PointCount"/> vzorků.
    /// </summary>
    private static int FindTrigger(ReadOnlySpan<float> samples)
    {
        int latest = samples.Length - PointCount;
        int earliest = Math.Max(1, latest - PointCount);
        for (int i = latest; i >= earliest; i--)
        {
            if (samples[i - 1] <= 0f && samples[i] > 0f)
            {
                return i;
            }
        }

        return latest;
    }

    private void DrawPass(float halfWidth, float intensity, float sharpness)
    {
        _gl!.Uniform1(_halfWidthLocation, halfWidth);
        _gl.Uniform1(_intensityLocation, intensity);
        _gl.Uniform1(_sharpnessLocation, sharpness);
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, PointCount * 2);
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

    private static Vector3 Hsv(float hue, float saturation, float value)
    {
        float h = hue * 6f;
        float c = value * saturation;
        float x = c * (1f - MathF.Abs(h % 2f - 1f));
        var rgb = (int)h switch
        {
            0 => new Vector3(c, x, 0),
            1 => new Vector3(x, c, 0),
            2 => new Vector3(0, c, x),
            3 => new Vector3(0, x, c),
            4 => new Vector3(x, 0, c),
            _ => new Vector3(c, 0, x),
        };
        return rgb + new Vector3(value - c);
    }
}
