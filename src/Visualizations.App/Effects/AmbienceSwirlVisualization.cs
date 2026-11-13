using System.Numerics;
using Silk.NET.OpenGL;
using Visualizations.App.Rendering;
using Visualizations.Core.Analysis;

namespace Visualizations.App.Effects;

/// <summary>
/// „Swirl“ z rodiny Ambience: kruhový průběh signálu, který se ve zpětné vazbě točí a rozpíná do víru.
/// Basy zrychlují rotaci, údery obraz „nafouknou“, stopy se pomalu přebarvují.
/// </summary>
internal sealed class AmbienceSwirlVisualization : FeedbackVisualization
{
    private const int PointCount = 512;
    private const float DecayTime = 0.55f;
    private const float HueShiftSpeed = 0.5f;

    private readonly Vector2[] _points = new Vector2[PointCount];
    private GlowLine? _line;

    protected override string WarpShader => """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uPrevious;
        uniform vec2 uResolution;
        uniform float uTime;
        uniform float uRotation;  // úhel otočení za tento snímek
        uniform float uZoom;      // < 1 → obraz se rozpíná od středu
        uniform float uDecay;     // útlum za tento snímek
        uniform float uHueShift;  // otočení odstínu za tento snímek (radiány)

        // Rotace barvy kolem šedé osy – mění odstín, ale nesnižuje sytost.
        vec3 rotateHue(vec3 c, float angle)
        {
            const vec3 axis = vec3(0.57735);
            float s = sin(angle);
            float k = cos(angle);
            return c * k + cross(axis, c) * s + axis * dot(axis, c) * (1.0 - k);
        }

        void main()
        {
            vec2 aspect = vec2(uResolution.x / uResolution.y, 1.0);
            vec2 p = (vUv - 0.5) * aspect;
            float r = length(p);
            // Víc točení u středu a pomalé vlnění po poloměru → víry místo prosté rotace.
            float angle = uRotation * (1.4 - r) * (1.0 + 0.5 * sin(r * 9.0 - uTime * 0.8));
            float s = sin(angle);
            float c = cos(angle);
            p = mat2(c, s, -s, c) * p * uZoom;
            p += 0.0025 * vec2(sin(p.y * 13.0 + uTime * 1.1), cos(p.x * 11.0 - uTime * 0.9));
            vec3 color = texture(uPrevious, p / aspect + 0.5).rgb * uDecay;
            fragColor = vec4(max(rotateHue(color, uHueShift), 0.0), 1.0);
        }
        """;

    public override void Load(GL gl)
    {
        base.Load(gl);
        _line = new GlowLine(gl);
    }

    public override void Dispose()
    {
        _line?.Dispose();
        base.Dispose();
    }

    protected override void SetWarpUniforms(ShaderProgram warp, in FrameContext frame)
    {
        var audio = frame.Audio;
        float dt = frame.DeltaTime;
        warp.Set("uRotation", (0.35f + 1.2f * audio.Bass) * dt);
        warp.Set("uZoom", MathF.Exp(-dt * (0.18f + 0.5f * audio.Bass + 1.5f * audio.Beat)));
        warp.Set("uDecay", MathF.Exp(-dt / DecayTime));
        warp.Set("uHueShift", HueShiftSpeed * dt);
    }

    protected override void Inject(in FrameContext frame)
    {
        if (_line is null)
        {
            return;
        }

        var audio = frame.Audio;
        float width = frame.Target.Width;
        float height = frame.Target.Height;
        var center = new Vector2(width, height) / 2f;
        float radius = height * (0.16f + 0.06f * audio.Bass);
        float gain = 2.5f * audio.Gain;
        float rotation = (float)(frame.Time * 0.3);

        // Zrcadlený průběh, aby se uzavřená křivka v místě spoje nelámala.
        var waveform = audio.Waveform;
        int half = PointCount / 2;
        int step = waveform.Length / half;
        for (int i = 0; i < PointCount; i++)
        {
            int sampleIndex = (i < half ? i : PointCount - 1 - i) * step;
            float value = Math.Clamp(waveform[sampleIndex] * gain, -1f, 1f);
            float angle = 2f * MathF.PI * i / PointCount + rotation;
            float r = radius * (1f + 0.35f * value);
            _points[i] = center + r * new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        // Čára se přičítá každý snímek a doznívá ~0,5 s, takže stačí slabá – jinak se obraz přepálí do bílé.
        var color = Colors.Hsv((float)(frame.Time * 0.05) + 0.4f * audio.Treble, 0.85f, 1f);
        _line.Draw(_points, closed: true, color, new Vector2(width, height), coreIntensity: 0.16f, glowIntensity: 0.05f);
    }
}
