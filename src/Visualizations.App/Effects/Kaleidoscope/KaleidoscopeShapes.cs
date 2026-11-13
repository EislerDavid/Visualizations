using System.Numerics;
using Visualizations.App.Rendering;

namespace Visualizations.App.Effects.Kaleidoscope;

/// <summary>
/// Kreslí tvar presetu (<see cref="KaleidoscopeShape"/>) jako zářící čáry do zpětné vazby.
/// Kaleidoskop zobrazuje jen jednu výseč (u 12 výsečí 15°), proto jsou tvary soustředné se středem
/// obrazu, nebo se jejich body do této výseče „přeloží“ stejně jako v shaderu (<see cref="Fold"/>).
/// </summary>
internal sealed class KaleidoscopeShapes
{
    private const float Tau = 2f * MathF.PI;

    private readonly Vector2[] _points = new Vector2[720];
    private readonly Random _random = new();
    private float _burstAngle;

    // Styl právě kresleného tvaru (nastaví Draw, používá Emit).
    private GlowLine _line = null!;
    private Vector2 _resolution;
    private Vector2 _center;
    private int _segments;
    private float _scale;
    private float _intensity;

    /// <param name="weight">Váha při přechodu mezi presety (0–1); násobí jas.</param>
    public void Draw(GlowLine line, KaleidoscopePreset preset, float weight, in FrameContext frame)
    {
        if (weight <= 0.001f)
        {
            return;
        }

        var audio = frame.Audio;
        float height = frame.Target.Height;
        _line = line;
        _resolution = new Vector2(frame.Target.Width, height);
        _center = _resolution / 2f;
        _segments = Math.Max(preset.Segments, 1);
        _scale = height / 600f;
        _intensity = preset.Intensity * weight * (0.6f + 0.8f * audio.Bass + 0.6f * audio.Beat);

        var input = new ShapeInput(audio.Waveform, audio.Bands)
        {
            Center = _resolution / 2f,
            Radius = preset.ShapeRadius * height,
            Amplitude = preset.ShapeAmplitude,
            Count = Math.Max(preset.ShapeCount, 1),
            Time = (float)frame.Time,
            Bass = audio.Bass,
            Treble = audio.Treble,
            Beat = audio.Beat,
            Gain = 2.5f * audio.Gain,
        };
        var color = Colors.Hsv(preset.BaseHue + input.Time * 0.03f + 0.15f * audio.Treble, 0.85f, 1f);

        switch (preset.Shape)
        {
            case KaleidoscopeShape.WaveLine: WaveLine(input, color); break;
            case KaleidoscopeShape.WaveRing: WaveRing(input, color); break;
            case KaleidoscopeShape.Rose: Rose(input, color); break;
            case KaleidoscopeShape.Star: Star(input, color); break;
            case KaleidoscopeShape.Spiral: Spiral(input, color); break;
            case KaleidoscopeShape.Spokes: Spokes(input, color); break;
            case KaleidoscopeShape.Orbits: Orbits(input, preset.BaseHue); break;
            case KaleidoscopeShape.Lissajous: Lissajous(input, color); break;
            case KaleidoscopeShape.Burst: Burst(input, color); break;
        }
    }

    private void WaveLine(in ShapeInput s, Vector3 color)
    {
        const int n = 256;
        float angle = s.Time * 0.2f;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var normal = new Vector2(-direction.Y, direction.X);
        for (int i = 0; i < n; i++)
        {
            float t = i / (n - 1f);
            float offset = s.Wave(t) * s.Amplitude * s.Radius;
            _points[i] = s.Center + direction * ((t * 2f - 1f) * s.Radius) + normal * offset;
        }

        Emit(n, closed: false, color);
    }

    private void WaveRing(in ShapeInput s, Vector3 color)
    {
        const int n = 360;
        const int half = n / 2;
        float radius = s.Radius * (1f + 0.5f * s.Bass);
        float rotation = s.Time * 0.3f;
        for (int i = 0; i < n; i++)
        {
            // Zrcadlený průběh, aby se kruh v místě spoje nelámal.
            float t = (i < half ? i : n - 1 - i) / (half - 1f);
            _points[i] = s.Center + Polar(radius * (1f + s.Amplitude * s.Wave(t)), Tau * i / n + rotation);
        }

        // Pulz s údery: z každého úderu odletí výrazný prstenec (tunel), jinak by se kruhy slily do mlhy.
        Emit(n, closed: true, color, intensityScale: 0.35f + 1.6f * s.Beat);
    }

    private void Rose(in ShapeInput s, Vector3 color)
    {
        const int n = 540;
        float rotation = s.Time * 0.1f;
        for (int i = 0; i < n; i++)
        {
            float angle = Tau * i / n;
            float petal = 0.35f + 0.65f * MathF.Abs(MathF.Cos(s.Count * angle / 2f));
            float spectrum = s.Band(MathF.Abs(2f * i / n - 1f));
            _points[i] = s.Center + Polar(s.Radius * petal * (1f + 1.5f * s.Amplitude * spectrum), angle + rotation);
        }

        Emit(n, closed: true, color);
    }

    private void Star(in ShapeInput s, Vector3 color)
    {
        const int perEdge = 8;
        int corners = Math.Min(2 * s.Count, _points.Length / perEdge);
        float outer = s.Radius * (1f + 2f * s.Amplitude * s.Bass);
        float inner = s.Radius * 0.42f * (1f + 2f * s.Amplitude * s.Treble);
        float rotation = s.Time * 0.3f;

        int o = 0;
        for (int j = 0; j < corners; j++)
        {
            var from = Polar(j % 2 == 0 ? outer : inner, Tau * j / corners + rotation);
            var to = Polar((j + 1) % 2 == 0 ? outer : inner, Tau * (j + 1) / corners + rotation);
            for (int e = 0; e < perEdge; e++)
            {
                _points[o++] = s.Center + Vector2.Lerp(from, to, e / (float)perEdge);
            }
        }

        Emit(o, closed: true, color);
    }

    private void Spiral(in ShapeInput s, Vector3 color)
    {
        const int n = 480;
        for (int i = 0; i < n; i++)
        {
            float t = i / (n - 1f);
            float radius = s.Radius * (0.08f + 0.92f * t) * (1f + s.Amplitude * s.Wave(t));
            _points[i] = s.Center + Polar(radius, t * s.Count * Tau + s.Time * 0.5f);
        }

        Emit(n, closed: false, color);
    }

    private void Spokes(in ShapeInput s, Vector3 color)
    {
        const int n = 48;
        for (int j = 0; j < s.Count; j++)
        {
            float angle = Tau * j / s.Count + s.Time * 0.15f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var normal = new Vector2(-direction.Y, direction.X);
            float length = s.Radius * (0.35f + 2f * s.Amplitude * s.Band(j / (float)s.Count));
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                float wiggle = s.Wave((t + j * 0.13f) % 1f) * s.Radius * 0.18f * s.Amplitude;
                _points[i] = s.Center + direction * (s.Radius * 0.12f + t * length) + normal * wiggle;
            }

            Emit(n, closed: false, color);
        }
    }

    private void Orbits(in ShapeInput s, float baseHue)
    {
        const int n = 20;
        for (int j = 0; j < s.Count; j++)
        {
            float orbit = s.Radius * (0.3f + 0.7f * (j + 1) / s.Count);
            float direction = j % 2 == 0 ? 1f : -1f;
            var center = s.Center + Polar(orbit, s.Time * (0.4f + 0.25f * j) * direction + j * 1.7f);
            float size = (3f + 26f * s.Amplitude * s.Band(j / (float)Math.Max(s.Count - 1, 1))) * _scale;
            for (int i = 0; i < n; i++)
            {
                _points[i] = center + Polar(size, Tau * i / n);
            }

            // Body obíhají – bez přeložení by výsečí proletěly jen občas a obraz by byl skoro černý.
            Emit(n, closed: true, Colors.Hsv(baseHue + s.Time * 0.03f + j * 0.3f / s.Count, 0.85f, 1f), fold: true);
        }
    }

    private void Lissajous(in ShapeInput s, Vector3 color)
    {
        const int n = 600;
        int a = s.Count;
        int b = Math.Max(s.Count - 1, 1);
        float phase = s.Time * 0.3f;
        float size = s.Radius * (1f + s.Amplitude * s.Bass);
        for (int i = 0; i < n; i++)
        {
            float t = Tau * i / n;
            float wobble = 1f + 0.15f * s.Amplitude * s.Wave(i / (float)n);
            _points[i] = s.Center + size * wobble * new Vector2(MathF.Sin(a * t + phase), MathF.Sin(b * t));
        }

        Emit(n, closed: true, color);
    }

    private void Burst(in ShapeInput s, Vector3 color)
    {
        // Stále svítící jádro, aby mezi údery nebyla tma.
        const int coreCount = 120;
        float coreRadius = s.Radius * (0.18f + 0.12f * s.Bass);
        for (int i = 0; i < coreCount; i++)
        {
            float t = (i < coreCount / 2 ? i : coreCount - 1 - i) / (coreCount / 2 - 1f);
            _points[i] = s.Center + Polar(coreRadius * (1f + 0.3f * s.Wave(t)), Tau * i / coreCount);
        }

        Emit(coreCount, closed: true, color, intensityScale: 0.3f);

        if (s.Beat >= 1f)
        {
            _burstAngle = (float)_random.NextDouble() * Tau;
        }

        if (s.Beat <= 0.01f)
        {
            return;
        }

        const int n = 16;
        for (int j = 0; j < s.Count; j++)
        {
            float angle = _burstAngle + Tau * j / s.Count;
            float inner = s.Radius * 0.1f;
            float outer = s.Radius * (0.5f + 0.8f * s.Bass);
            for (int i = 0; i < n; i++)
            {
                _points[i] = s.Center + Polar(inner + (outer - inner) * i / (n - 1f), angle);
            }

            Emit(n, closed: false, color, intensityScale: s.Beat, fold: true);
        }
    }

    /// <param name="fold">Přeložit body do zobrazované výseče (pro tvary, které nejsou soustředné).</param>
    private void Emit(int count, bool closed, Vector3 color, float intensityScale = 1f, bool fold = false)
    {
        if (fold)
        {
            for (int i = 0; i < count; i++)
            {
                _points[i] = Fold(_points[i]);
            }
        }

        float intensity = _intensity * intensityScale;
        _line.Draw(
            _points.AsSpan(0, count),
            closed,
            color,
            _resolution,
            coreHalfWidth: 1.6f * _scale,
            glowHalfWidth: 7f * _scale,
            coreIntensity: intensity,
            glowIntensity: intensity * 0.35f);
    }

    private static Vector2 Polar(float radius, float angle) => radius * new Vector2(MathF.Cos(angle), MathF.Sin(angle));

    /// <summary>Stejné zrcadlení do výseče jako <c>fold()</c> v shaderu kaleidoskopu (spojité, čáry se nelámou).</summary>
    private Vector2 Fold(Vector2 point)
    {
        var offset = point - _center;
        float wedge = Tau / _segments;
        float angle = MathF.Atan2(offset.Y, offset.X);
        float inWedge = angle - wedge * MathF.Floor(angle / wedge);
        float folded = MathF.Abs(inWedge - 0.5f * wedge) - 0.25f * wedge;
        return _center + Polar(offset.Length(), folded);
    }

    /// <summary>Zvukové vstupy a rozměry tvaru pro jeden snímek.</summary>
    private readonly ref struct ShapeInput(ReadOnlySpan<float> waveform, ReadOnlySpan<float> bands)
    {
        private readonly ReadOnlySpan<float> _waveform = waveform;
        private readonly ReadOnlySpan<float> _bands = bands;

        public Vector2 Center { get; init; }

        public float Radius { get; init; }

        public float Amplitude { get; init; }

        public int Count { get; init; }

        public float Time { get; init; }

        public float Bass { get; init; }

        public float Treble { get; init; }

        public float Beat { get; init; }

        public float Gain { get; init; }

        /// <summary>Průběh signálu v poloze <paramref name="t"/> (0–1), zesílený a oříznutý do −1..1.</summary>
        public float Wave(float t) =>
            Math.Clamp(_waveform[(int)(Math.Clamp(t, 0f, 1f) * (_waveform.Length - 1))] * Gain, -1f, 1f);

        /// <summary>Úroveň spektra v poloze <paramref name="t"/> (0 = basy, 1 = výšky).</summary>
        public float Band(float t) => _bands[(int)(Math.Clamp(t, 0f, 1f) * (_bands.Length - 1))];
    }
}
