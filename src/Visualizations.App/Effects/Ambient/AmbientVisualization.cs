using Silk.NET.OpenGL;
using Visualizations.App.Rendering;

namespace Visualizations.App.Effects.Ambient;

/// <summary>
/// Ambientní animace (rodina Ambience): měkká zpětná vazba bez zrcadlení – víření, proudění ze šumu,
/// drift, rozostření, vlnky, hvězdy – a barevné palety. Vzhled určuje <see cref="AmbientPreset"/>;
/// na jiný ambientní preset přechází plynulou proměnou parametrů (<see cref="IMorphable"/>).
/// </summary>
internal sealed class AmbientVisualization(AmbientPreset preset) : FeedbackVisualization, IMorphable
{
    private const string WarpGlsl = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uPrevious;
        uniform vec2 uResolution;
        uniform float uTime;
        uniform float uZoom;        // < 1 → obraz se rozpíná od středu
        uniform float uRotation;    // otočení za tento snímek
        uniform float uVortex;      // 0 = točí se celý obraz, 1 = víc u středu
        uniform float uVortexWave;
        uniform float uWobble;
        uniform float uRipple;
        uniform vec2 uDrift;        // posun za tento snímek
        uniform float uFlow;        // síla proudění za tento snímek
        uniform float uFlowScale;
        uniform float uFlowSpeed;
        uniform float uBlur;        // rozostření v pixelech
        uniform float uDecay;
        uniform float uHueShift;
        uniform float uStars;

        float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

        // Gradientní (Perlinův) šum. Hodnotový šum má na hranách mřížky nulovou derivaci, takže by
        // proudění teklo podél mřížky a zpětná vazba by z něj časem vykreslila obdélníky.
        float noise(vec2 p)
        {
            vec2 i = floor(p);
            vec2 f = fract(p);
            vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
            float a00 = hash(i) * 6.2831853;
            float a10 = hash(i + vec2(1.0, 0.0)) * 6.2831853;
            float a01 = hash(i + vec2(0.0, 1.0)) * 6.2831853;
            float a11 = hash(i + vec2(1.0, 1.0)) * 6.2831853;
            float n00 = dot(vec2(cos(a00), sin(a00)), f);
            float n10 = dot(vec2(cos(a10), sin(a10)), f - vec2(1.0, 0.0));
            float n01 = dot(vec2(cos(a01), sin(a01)), f - vec2(0.0, 1.0));
            float n11 = dot(vec2(cos(a11), sin(a11)), f - vec2(1.0, 1.0));
            return mix(mix(n00, n10, u.x), mix(n01, n11, u.x), u.y);
        }

        // Vírové pole (curl šumu): proudí jako kapalina, nikde se nehromadí ani neřídne.
        vec2 curl(vec2 p)
        {
            const float e = 0.02;
            float dx = noise(p + vec2(e, 0.0)) - noise(p - vec2(e, 0.0));
            float dy = noise(p + vec2(0.0, e)) - noise(p - vec2(0.0, e));
            return vec2(dy, -dx) / (2.0 * e);
        }

        vec2 flow(vec2 p)
        {
            // Čas se zalamuje, aby velké souřadnice nezhoršily přesnost hashe při dlouhém běhu.
            float t = mod(uTime * uFlowSpeed, 512.0);
            vec2 q = p * uFlowScale + vec2(t, 0.7 * t);
            // Druhá oktáva je natočená, aby se víry neřadily do mřížky. Vektor proudění se musí natočit
            // zpátky (transpose), jinak pole přestane být nestlačitelné: obraz by se na některých místech
            // smršťoval do ostrých hran a jinde by vznikaly černé díry s ostrým okrajem.
            const mat2 turn = mat2(0.8, 0.6, -0.6, 0.8);
            return curl(q) + 0.5 * (transpose(turn) * curl(turn * q * 2.3 + 17.0));
        }

        mat2 rotation(float angle)
        {
            float s = sin(angle);
            float c = cos(angle);
            return mat2(c, s, -s, c);
        }

        // Rotace barvy kolem šedé osy – mění odstín, ale nesnižuje sytost.
        vec3 rotateHue(vec3 c, float angle)
        {
            const vec3 axis = vec3(0.57735);
            float s = sin(angle);
            float k = cos(angle);
            return c * k + cross(axis, c) * s + axis * dot(axis, c) * (1.0 - k);
        }

        // Mimo obraz nesmí být černá s ostrou hranou – rotace, proudění a drift by ji snímek po snímku
        // zanášely do obrazu jako ostré rovné zlomy. Okrajový pixel (ClampToEdge) proto plynule zhasne.
        float outsideFade(vec2 uv)
        {
            vec2 d = max(max(-uv, uv - 1.0), 0.0);
            return 1.0 - smoothstep(0.0, 0.03, max(d.x, d.y));
        }

        // Vzorek předchozího snímku s rozostřením.
        vec3 sampleSoft(vec2 p, vec2 aspect)
        {
            vec2 uv = p / aspect + 0.5;
            vec2 o = uBlur / uResolution;
            vec3 c = texture(uPrevious, uv).rgb * 0.4
                   + texture(uPrevious, uv + vec2(o.x, 0.0)).rgb * 0.15
                   + texture(uPrevious, uv - vec2(o.x, 0.0)).rgb * 0.15
                   + texture(uPrevious, uv + vec2(0.0, o.y)).rgb * 0.15
                   + texture(uPrevious, uv - vec2(0.0, o.y)).rgb * 0.15;
            return c * outsideFade(uv);
        }

        vec3 stars(vec2 p)
        {
            vec2 grid = p * 40.0;
            vec2 cell = floor(grid);
            float h = hash(cell);
            vec2 offset = vec2(hash(cell + 1.7), hash(cell + 3.1)) - 0.5;
            float d = length(fract(grid) - 0.5 - offset * 0.7);
            float twinkle = 0.5 + 0.5 * sin(uTime * (2.0 + h * 4.0) + h * 30.0);
            float star = step(0.88, h) * (1.0 - smoothstep(0.0, 0.09, d)) * twinkle;
            return star * mix(vec3(0.7, 0.8, 1.0), vec3(1.0, 0.9, 0.7), hash(cell + 5.0));
        }

        // Váha otáčení a přiblížení: u okrajů obrazu plynule klesá k nule, takže tuhý pohyb nikdy nevzorkuje
        // mimo obraz – rovná hrana obrazu by se jinak zanášela dovnitř jako rovné zlomy. Drift a proudění se
        // netlumí: zbrzdění u okraje by obraz stlačilo a vytvořilo ostré hrany rovnoběžné s okrajem.
        float edgeWeight(vec2 p, vec2 aspect)
        {
            vec2 distance = 0.5 * aspect - abs(p);
            return smoothstep(0.0, 0.12, min(distance.x, distance.y));
        }

        void main()
        {
            vec2 aspect = vec2(uResolution.x / uResolution.y, 1.0);
            vec2 p = (vUv - 0.5) * aspect;
            float r = length(p);

            float angle = uRotation * mix(1.0, 1.4 - r, uVortex) * (1.0 + uVortexWave * sin(r * 9.0 - uTime * 0.8));
            vec2 q = rotation(angle) * p * uZoom;
            q *= 1.0 + uRipple * sin(r * 40.0 - uTime * 3.0);
            q += uWobble * vec2(sin(q.y * 13.0 + uTime * 1.1), cos(q.x * 11.0 - uTime * 0.9));
            q = p + (q - p) * edgeWeight(p, aspect);
            q -= uDrift;
            if (uFlow > 0.0)
            {
                q += uFlow * flow(q);
            }

            vec3 color = rotateHue(sampleSoft(q, aspect) * uDecay, uHueShift);
            if (uStars > 0.0)
            {
                color += stars(p) * uStars;
            }

            fragColor = vec4(max(color, 0.0), 1.0);
        }
        """;

    private const string PresentGlsl = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uFrame;
        uniform vec2 uResolution;
        uniform float uExposure;
        uniform float uPaletteMix;
        uniform float uVignette;
        uniform vec3 uPalette0;
        uniform vec3 uPalette1;
        uniform vec3 uPalette2;
        uniform vec3 uPalette3;

        vec3 palette(float t)
        {
            t = clamp(t, 0.0, 1.0) * 3.0;
            if (t < 1.0) return mix(uPalette0, uPalette1, t);
            if (t < 2.0) return mix(uPalette1, uPalette2, t - 1.0);
            return mix(uPalette2, uPalette3, t - 2.0);
        }

        void main()
        {
            vec3 c = 1.0 - exp(-texture(uFrame, vUv).rgb * uExposure);
            c = mix(c, palette(max(c.r, max(c.g, c.b))), uPaletteMix);
            vec2 p = (vUv - 0.5) * vec2(uResolution.x / uResolution.y, 1.0);
            c *= mix(1.0, 1.0 - smoothstep(0.25, 1.1, length(p)), uVignette);
            fragColor = vec4(c, 1.0);
        }
        """;

    private readonly PresetMorph<AmbientPreset> _morph = new(preset, AmbientPreset.Lerp);
    private readonly AmbientShapes _shapes = new();
    private GlowLine? _line;

    protected override string WarpShader => WarpGlsl;

    protected override string PresentShader => PresentGlsl;

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
        if (target.MorphData is not AmbientPreset preset)
        {
            return false;
        }

        _morph.Start(preset, seconds);
        return true;
    }

    protected override void SetWarpUniforms(ShaderProgram warp, in FrameContext frame)
    {
        float dt = frame.DeltaTime;
        _morph.Advance(dt);
        var p = _morph.Current;
        var audio = frame.Audio;

        warp.Set("uZoom", MathF.Exp(-dt * (p.ZoomSpeed + p.BassZoom * audio.Bass + p.BeatPunch * audio.Beat)));
        warp.Set("uRotation", (p.RotationSpeed + p.BassSpin * audio.Bass) * dt);
        warp.Set("uVortex", p.Vortex);
        warp.Set("uVortexWave", p.VortexWave);
        warp.Set("uWobble", p.Wobble);
        warp.Set("uRipple", p.Ripple);
        warp.Set("uDrift", p.Drift * dt);
        warp.Set("uFlow", p.Flow * dt);
        warp.Set("uFlowScale", p.FlowScale);
        warp.Set("uFlowSpeed", p.FlowSpeed);
        warp.Set("uBlur", p.Blur * frame.Target.Height / 600f);
        warp.Set("uDecay", MathF.Exp(-dt / p.DecayTime));
        warp.Set("uHueShift", p.HueSpeed * dt);
        warp.Set("uStars", p.Stars * dt * 4f * (1f + audio.Beat));
    }

    protected override void Inject(in FrameContext frame)
    {
        if (_line is null)
        {
            return;
        }

        if (_morph.Blend < 1f)
        {
            _shapes.Draw(_line, _morph.From, 1f - _morph.Blend, frame);
        }

        _shapes.Draw(_line, _morph.To, _morph.Blend, frame);
    }

    protected override void SetPresentUniforms(ShaderProgram present, in FrameContext frame)
    {
        var p = _morph.Current;
        present.Set("uExposure", p.Exposure);
        present.Set("uPaletteMix", p.PaletteMix);
        present.Set("uVignette", p.Vignette);
        present.Set("uPalette0", p.Palette.C0);
        present.Set("uPalette1", p.Palette.C1);
        present.Set("uPalette2", p.Palette.C2);
        present.Set("uPalette3", p.Palette.C3);
    }
}
