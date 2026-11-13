using Silk.NET.OpenGL;
using Visualizations.App.Rendering;

namespace Visualizations.App.Effects.Kaleidoscope;

/// <summary>
/// Kaleidoskop (rodina Battery): zpětná vazba (rotace, přiblížení, víření, přebarvování) + tvar podle
/// zvuku + zrcadlení do výsečí. Vzhled určuje <see cref="KaleidoscopePreset"/>. Na jiný kaleidoskop
/// přechází plynulou proměnou parametrů (<see cref="IMorphable"/>), ne prolnutím dvou obrazů.
/// </summary>
internal sealed class KaleidoscopeVisualization(KaleidoscopePreset preset) : FeedbackVisualization, IMorphable
{
    private const string Header = "#version 330 core\n";

    private const string CommonGlsl = """
        const float TAU = 6.2831853;

        // Zrcadlení do výseče: bod se převede do základní výseče kolem osy +x. Sousední výseče jsou
        // zrcadlové, takže na hranicích nejsou švy. Výseč je vycentrovaná na ose, aby se vešla do obrazu.
        vec2 fold(vec2 p, float segments)
        {
            float wedge = TAU / max(segments, 1.0);
            float angle = abs(mod(atan(p.y, p.x), wedge) - 0.5 * wedge) - 0.25 * wedge;
            return length(p) * vec2(cos(angle), sin(angle));
        }

        // Vzorek v souřadnicích se středem uprostřed obrazu (výška = 1); mimo obraz je černo.
        vec3 sampleAt(sampler2D image, vec2 p, vec2 aspect)
        {
            vec2 uv = p / aspect + 0.5;
            vec2 inside = step(vec2(0.0), uv) * step(uv, vec2(1.0));
            return texture(image, uv).rgb * inside.x * inside.y;
        }

        mat2 rotation(float angle)
        {
            float s = sin(angle);
            float c = cos(angle);
            return mat2(c, s, -s, c);
        }
        """;

    private const string WarpMain = """
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uPrevious;
        uniform vec2 uResolution;
        uniform float uTime;
        uniform float uZoom;        // < 1 → obraz se rozpíná od středu
        uniform float uRotation;    // otočení za tento snímek
        uniform float uSwirl;       // otočení navíc u středu za tento snímek
        uniform float uWobble;
        uniform float uDecay;       // útlum za tento snímek
        uniform float uHueShift;    // otočení odstínu za tento snímek
        uniform float uSegmentsA;
        uniform float uSegmentsB;
        uniform float uSegmentMix;  // přechod mezi počty výsečí A → B
        uniform float uWarpFold;    // 0–1: jak moc zrcadlit už ve zpětné vazbě

        // Rotace barvy kolem šedé osy – mění odstín, ale nesnižuje sytost.
        vec3 rotateHue(vec3 c, float angle)
        {
            const vec3 axis = vec3(0.57735);
            float s = sin(angle);
            float k = cos(angle);
            return c * k + cross(axis, c) * s + axis * dot(axis, c) * (1.0 - k);
        }

        vec2 move(vec2 p)
        {
            float r = length(p);
            p = rotation(uRotation + uSwirl * max(1.0 - r, 0.0)) * p * uZoom;
            return p + uWobble * vec2(sin(p.y * 7.0 + uTime * 0.9), cos(p.x * 6.0 - uTime * 0.7));
        }

        void main()
        {
            vec2 aspect = vec2(uResolution.x / uResolution.y, 1.0);
            vec2 p = (vUv - 0.5) * aspect;
            vec3 color = sampleAt(uPrevious, move(p), aspect);
            if (uWarpFold > 0.001)
            {
                vec3 folded = mix(sampleAt(uPrevious, move(fold(p, uSegmentsA)), aspect),
                                  sampleAt(uPrevious, move(fold(p, uSegmentsB)), aspect), uSegmentMix);
                color = mix(color, folded, uWarpFold);
            }

            fragColor = vec4(max(rotateHue(color * uDecay, uHueShift), 0.0), 1.0);
        }
        """;

    private const string PresentMain = """
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uFrame;
        uniform vec2 uResolution;
        uniform float uSegmentsA;
        uniform float uSegmentsB;
        uniform float uSegmentMix;
        uniform float uViewAngle;
        uniform float uSaturation;

        // Mírné přiblížení, aby rohy obrazovky po zrcadlení nesahaly mimo obraz.
        const float VIEW_SCALE = 0.85;

        void main()
        {
            vec2 aspect = vec2(uResolution.x / uResolution.y, 1.0);
            vec2 p = rotation(uViewAngle) * ((vUv - 0.5) * aspect * VIEW_SCALE);
            vec3 color = mix(sampleAt(uFrame, fold(p, uSegmentsA), aspect),
                             sampleAt(uFrame, fold(p, uSegmentsB), aspect), uSegmentMix);
            color = 1.0 - exp(-color * 1.6);
            float luma = dot(color, vec3(0.299, 0.587, 0.114));
            fragColor = vec4(clamp(mix(vec3(luma), color, uSaturation), 0.0, 1.0), 1.0);
        }
        """;

    private readonly KaleidoscopeShapes _shapes = new();
    private KaleidoscopePreset _from = preset;
    private KaleidoscopePreset _to = preset;
    private KaleidoscopePreset _current = preset;
    private float _progress = 1f;
    private float _duration = 1f;
    private float _blend = 1f;
    private float _viewAngle;
    private GlowLine? _line;

    protected override string WarpShader => Header + CommonGlsl + "\n" + WarpMain;

    protected override string PresentShader => Header + CommonGlsl + "\n" + PresentMain;

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

    public bool TryMorphTo(VisualizationInfo target, float seconds)
    {
        if (target.MorphData is not KaleidoscopePreset preset)
        {
            return false;
        }

        // Přerušená proměna: pokračovat z aktuálního mezistavu (počet výsečí a tvar od převažujícího presetu).
        _from = _progress >= 1f ? _to
            : _blend < 0.5f ? KaleidoscopePreset.Lerp(_from, _to, _blend)
            : KaleidoscopePreset.Lerp(_to, _from, 1f - _blend);
        _to = preset;
        _duration = seconds;
        _progress = seconds > 0f ? 0f : 1f;
        _blend = _progress >= 1f ? 1f : 0f;
        return true;
    }

    protected override void SetWarpUniforms(ShaderProgram warp, in FrameContext frame)
    {
        float dt = frame.DeltaTime;
        if (_progress < 1f)
        {
            _progress = MathF.Min(1f, _progress + dt / _duration);
        }

        _blend = _progress * _progress * (3f - 2f * _progress);
        _current = _progress >= 1f ? _to : KaleidoscopePreset.Lerp(_from, _to, _blend);
        var p = _current;
        var audio = frame.Audio;
        _viewAngle += p.ViewRotationSpeed * dt;

        warp.Set("uZoom", MathF.Exp(-dt * (p.ZoomSpeed + p.BeatPunch * audio.Beat)));
        warp.Set("uRotation", (p.RotationSpeed + p.BassSpin * audio.Bass) * dt);
        warp.Set("uSwirl", p.Swirl * dt);
        warp.Set("uWobble", p.Wobble);
        warp.Set("uDecay", MathF.Exp(-dt / p.DecayTime));
        warp.Set("uHueShift", p.HueSpeed * dt);
        warp.Set("uWarpFold", (_from.FoldInWarp ? 1f - _blend : 0f) + (_to.FoldInWarp ? _blend : 0f));
        SetSegments(warp);
    }

    protected override void Inject(in FrameContext frame)
    {
        if (_line is null)
        {
            return;
        }

        if (_blend < 1f)
        {
            _shapes.Draw(_line, _from, 1f - _blend, frame);
        }

        _shapes.Draw(_line, _to, _blend, frame);
    }

    protected override void SetPresentUniforms(ShaderProgram present, in FrameContext frame)
    {
        SetSegments(present);
        present.Set("uViewAngle", _viewAngle);
        present.Set("uSaturation", _current.Saturation);
    }

    private void SetSegments(ShaderProgram program)
    {
        program.Set("uSegmentsA", (float)_from.Segments);
        program.Set("uSegmentsB", (float)_to.Segments);
        program.Set("uSegmentMix", _blend);
    }
}
