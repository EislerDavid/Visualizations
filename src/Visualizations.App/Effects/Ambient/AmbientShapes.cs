using System.Numerics;
using Visualizations.App.Rendering;

namespace Visualizations.App.Effects.Ambient;

/// <summary>
/// Kreslí tvar ambientního presetu (<see cref="AmbientShape"/>) jako zářící čáry do zpětné vazby.
/// Kruhy na vodě, bubliny a kapky jsou částice s vlastním životem, které vznikají hlavně na údery.
/// </summary>
internal sealed class AmbientShapes
{
    private const float Tau = 2f * MathF.PI;
    private const int MaxDrops = 40;

    private readonly Vector2[] _points = new Vector2[600];
    private readonly Random _random = new();
    private readonly ParticleSet _ripples = new();
    private readonly ParticleSet _bubbles = new();
    private readonly ParticleSet _drops = new();

    // Styl právě kresleného tvaru (nastaví Draw, používá Emit).
    private GlowLine _line = null!;
    private Vector2 _resolution;
    private float _scale;
    private float _intensity;

    /// <param name="weight">Váha při přechodu mezi presety (0–1); násobí jas.</param>
    public void Draw(GlowLine line, AmbientPreset preset, float weight, in FrameContext frame)
    {
        if (weight <= 0.001f)
        {
            return;
        }

        var audio = frame.Audio;
        float width = frame.Target.Width;
        float height = frame.Target.Height;
        _line = line;
        _resolution = new Vector2(width, height);
        _scale = height / 600f;
        _intensity = preset.Intensity * weight;

        var input = new ShapeInput(audio.Waveform, audio.Bands)
        {
            Size = _resolution,
            Radius = preset.ShapeRadius * height,
            Amplitude = preset.ShapeAmplitude,
            Count = Math.Max(preset.ShapeCount, 1),
            ShapeY = preset.ShapeY,
            BaseHue = preset.BaseHue,
            Time = (float)frame.Time,
            DeltaTime = frame.DeltaTime,
            Bass = audio.Bass,
            Treble = audio.Treble,
            Beat = audio.Beat,
            Level = audio.Level,
            Gain = 2.5f * audio.Gain,
        };

        switch (preset.Shape)
        {
            case AmbientShape.WaveRing: WaveRing(input); break;
            case AmbientShape.WaveLines: WaveLines(input); break;
            case AmbientShape.Ripples: Ripples(input, frame.Time); break;
            case AmbientShape.Bubbles: Bubbles(input, frame.Time); break;
            case AmbientShape.Drops: Drops(input, frame.Time); break;
            case AmbientShape.Orbs: Orbs(input); break;
        }
    }

    private void WaveRing(in ShapeInput s)
    {
        const int n = 512;
        const int half = n / 2;
        float radius = s.Radius * (1f + 0.375f * s.Bass);
        float rotation = s.Time * 0.3f;
        for (int i = 0; i < n; i++)
        {
            // Zrcadlený průběh, aby se kruh v místě spoje nelámal.
            float t = (i < half ? i : n - 1 - i) / (half - 1f);
            _points[i] = s.Center + Polar(radius * (1f + s.Amplitude * s.Wave(t)), Tau * i / n + rotation);
        }

        Emit(n, closed: true, Colors.Hsv(s.BaseHue + s.Time * 0.05f + 0.4f * s.Treble, 0.85f, 1f));
    }

    private void WaveLines(in ShapeInput s)
    {
        const int n = 256;
        float window = 1f - 0.07f * (s.Count - 1);
        for (int k = 0; k < s.Count; k++)
        {
            float y = s.ShapeY * s.Size.Y + (k - (s.Count - 1) / 2f) * 0.07f * s.Size.Y;
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                float offset = s.Wave(t * window + k * 0.07f) * s.Amplitude * s.Size.Y * 0.5f;
                _points[i] = new Vector2(t * s.Size.X, y + offset);
            }

            Emit(n, closed: false, Colors.Hsv(s.BaseHue + k * 0.08f + s.Time * 0.03f + 0.2f * s.Treble, 0.85f, 1f));
        }
    }

    private void Ripples(in ShapeInput s, double time)
    {
        if (_ripples.BeginFrame(time, s.DeltaTime))
        {
            bool quiet = _ripples.SinceSpawn > 1.2f && s.Level > 0.01f;
            if ((s.Beat >= 1f || quiet) && _ripples.Count < s.Count)
            {
                _ripples.Add(new Particle
                {
                    Position = RandomPoint(s.Size, margin: 0.15f),
                    Life = 2.2f,
                    Size = s.Radius * (0.6f + 0.8f * NextFloat()) * (1f + s.Bass),
                    Hue = s.BaseHue + 0.2f * NextFloat(),
                });
            }
        }

        const int n = 64;
        foreach (var ripple in _ripples.Items)
        {
            float progress = ripple.Age / ripple.Life;
            float radius = ripple.Size * (1f - (1f - progress) * (1f - progress));
            for (int i = 0; i < n; i++)
            {
                float wave = 1f + 0.1f * s.Amplitude * s.Wave(i / (n - 1f));
                _points[i] = ripple.Position + Polar(radius * wave, Tau * i / n);
            }

            float fade = MathF.Pow(1f - progress, 1.5f) * (0.6f + s.Bass);
            Emit(n, closed: true, Colors.Hsv(ripple.Hue + s.Time * 0.03f, 0.8f, 1f), intensityScale: fade);
        }
    }

    private void Bubbles(in ShapeInput s, double time)
    {
        if (_bubbles.BeginFrame(time, s.DeltaTime))
        {
            foreach (var bubble in _bubbles.Items)
            {
                bubble.Position += bubble.Velocity * s.DeltaTime;
                bubble.Position.X += MathF.Sin(bubble.Age * 2f + bubble.Hue * 10f) * s.Size.Y * 0.02f * s.DeltaTime;
            }

            float top = s.Size.Y;
            _bubbles.RemoveWhere(b => b.Position.Y - b.Size > top);
            _bubbles.Budget = MathF.Min(3f, _bubbles.Budget + s.DeltaTime * (0.8f + 3f * s.Bass) + (s.Beat >= 1f ? 2f : 0f));
            while (_bubbles.Budget >= 1f && _bubbles.Count < s.Count)
            {
                float size = s.Radius * (0.5f + NextFloat()) * (1f + s.Amplitude * s.Bass);
                _bubbles.Add(new Particle
                {
                    Position = new Vector2((0.1f + 0.8f * NextFloat()) * s.Size.X, -size),
                    Velocity = new Vector2(0f, (0.06f + 0.08f * NextFloat()) * s.Size.Y),
                    Life = 8f,
                    Size = size,
                    Hue = s.BaseHue + 0.15f * NextFloat(),
                });
                _bubbles.Budget -= 1f;
            }
        }

        const int n = 28;
        foreach (var bubble in _bubbles.Items)
        {
            // Měkká skvrna (kroužek o polovičním poloměru s tlustou září) – ostrý obrys by za sebou
            // při stoupání táhl ostré stopy.
            for (int i = 0; i < n; i++)
            {
                _points[i] = bubble.Position + Polar(bubble.Size * 0.5f, Tau * i / n);
            }

            float fade = MathF.Min(1f, bubble.Age / 0.4f) * MathF.Min(1f, (bubble.Life - bubble.Age) / 1f);
            EmitSoft(n, Colors.Hsv(bubble.Hue, 0.8f, 1f), intensityScale: fade * (0.7f + 0.6f * s.Bass), halfWidth: bubble.Size * 0.48f);
        }
    }

    private void Drops(in ShapeInput s, double time)
    {
        if (_drops.BeginFrame(time, s.DeltaTime))
        {
            _drops.Budget = MathF.Min(2f, _drops.Budget + s.DeltaTime * s.Level * 15f);
            int spawn = (s.Beat >= 1f ? s.Count : 0) + (int)_drops.Budget;
            _drops.Budget -= (int)_drops.Budget;
            for (int i = 0; i < spawn && _drops.Count < MaxDrops; i++)
            {
                _drops.Add(new Particle
                {
                    Position = RandomPoint(s.Size, margin: 0.1f),
                    Life = 0.35f,
                    Size = s.Radius * (0.6f + 0.8f * NextFloat()),
                    Hue = NextFloat(),
                });
            }
        }

        const int n = 16;
        foreach (var drop in _drops.Items)
        {
            for (int i = 0; i < n; i++)
            {
                _points[i] = drop.Position + Polar(drop.Size * 0.5f, Tau * i / n);
            }

            // Malý kroužek s tlustou čárou působí jako plná kapka. Šířka nesmí přesáhnout poloměr,
            // jinak se pruh trojúhelníků překříží přes střed a vzniknou ostny.
            float radius = drop.Size * 0.5f;
            EmitSoft(n, Colors.Hsv(drop.Hue, 0.8f, 1f), intensityScale: 1f - drop.Age / drop.Life, halfWidth: radius * 0.95f);
        }
    }

    private void Orbs(in ShapeInput s)
    {
        const int n = 32;
        for (int j = 0; j < s.Count; j++)
        {
            float fx = 0.13f + 0.05f * j;
            float fy = (0.11f + 0.04f * j) * 1.3f;
            var position = s.Center + new Vector2(
                0.32f * s.Size.X * MathF.Sin(s.Time * fx + j * 1.3f),
                0.30f * s.Size.Y * MathF.Sin(s.Time * fy + j * 2.1f));
            float level = s.Band(j / (float)Math.Max(s.Count - 1, 1));
            float size = s.Radius * (0.25f + s.Amplitude * level);
            for (int i = 0; i < n; i++)
            {
                _points[i] = position + Polar(size * 0.5f, Tau * i / n);
            }

            // Šířka čáry do poloměru (size / 2), jinak vzniknou ostny.
            EmitSoft(n, Colors.Hsv(s.BaseHue + j * 0.6f / s.Count + s.Time * 0.02f, 0.75f, 1f),
                intensityScale: 0.4f + 1.2f * level, halfWidth: size * 0.47f);
        }
    }

    /// <summary>
    /// Uzavřený tvar jen s měkkou září, bez ostrého jádra. Pro pohybující se skvrny (koule, kapky):
    /// ostrý obrys by za sebou ve zpětné vazbě nechával rovné ostré stopy.
    /// </summary>
    private void EmitSoft(int count, Vector3 color, float intensityScale, float halfWidth)
    {
        float intensity = _intensity * intensityScale;
        _line.Draw(
            _points.AsSpan(0, count),
            closed: true,
            color,
            _resolution,
            coreHalfWidth: halfWidth,
            glowHalfWidth: halfWidth,
            coreIntensity: 0f,
            glowIntensity: intensity * 1.3f);
    }

    private void Emit(int count, bool closed, Vector3 color, float intensityScale = 1f, float? coreHalfWidth = null, float? glowHalfWidth = null)
    {
        float intensity = _intensity * intensityScale;
        _line.Draw(
            _points.AsSpan(0, count),
            closed,
            color,
            _resolution,
            coreHalfWidth: coreHalfWidth ?? 1.8f * _scale,
            glowHalfWidth: glowHalfWidth ?? 9f * _scale,
            coreIntensity: intensity,
            glowIntensity: intensity * 0.31f);
    }

    private Vector2 RandomPoint(Vector2 size, float margin) => new(
        (margin + (1f - 2f * margin) * NextFloat()) * size.X,
        (margin + (1f - 2f * margin) * NextFloat()) * size.Y);

    private float NextFloat() => (float)_random.NextDouble();

    private static Vector2 Polar(float radius, float angle) => radius * new Vector2(MathF.Cos(angle), MathF.Sin(angle));

    private sealed class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Age;
        public float Life;
        public float Size;
        public float Hue;
    }

    /// <summary>Částice jednoho tvaru; stárnou jednou za snímek, i když se tvar při přechodu kreslí dvakrát.</summary>
    private sealed class ParticleSet
    {
        private readonly List<Particle> _items = [];
        private double _updatedAt = double.NaN;

        public IReadOnlyList<Particle> Items => _items;

        public int Count => _items.Count;

        /// <summary>Čas od posledního zrození částice.</summary>
        public float SinceSpawn { get; private set; } = float.MaxValue;

        /// <summary>Nastřádaný „nárok“ na zrození částic při plynulém vzniku.</summary>
        public float Budget { get; set; }

        /// <summary>Zestárne částice a odstraní mrtvé; vrátí <c>false</c>, pokud už tento snímek proběhl.</summary>
        public bool BeginFrame(double time, float deltaTime)
        {
            if (time == _updatedAt)
            {
                return false;
            }

            _updatedAt = time;
            SinceSpawn += deltaTime;
            foreach (var particle in _items)
            {
                particle.Age += deltaTime;
            }

            _items.RemoveAll(p => p.Age >= p.Life);
            return true;
        }

        public void Add(Particle particle)
        {
            _items.Add(particle);
            SinceSpawn = 0f;
        }

        public void RemoveWhere(Predicate<Particle> predicate) => _items.RemoveAll(predicate);
    }

    /// <summary>Zvukové vstupy a rozměry tvaru pro jeden snímek.</summary>
    private readonly ref struct ShapeInput(ReadOnlySpan<float> waveform, ReadOnlySpan<float> bands)
    {
        private readonly ReadOnlySpan<float> _waveform = waveform;
        private readonly ReadOnlySpan<float> _bands = bands;

        public Vector2 Size { get; init; }

        public Vector2 Center => Size / 2f;

        public float Radius { get; init; }

        public float Amplitude { get; init; }

        public int Count { get; init; }

        public float ShapeY { get; init; }

        public float BaseHue { get; init; }

        public float Time { get; init; }

        public float DeltaTime { get; init; }

        public float Bass { get; init; }

        public float Treble { get; init; }

        public float Beat { get; init; }

        public float Level { get; init; }

        public float Gain { get; init; }

        /// <summary>Průběh signálu v poloze <paramref name="t"/> (0–1), zesílený a oříznutý do −1..1.</summary>
        public float Wave(float t) =>
            Math.Clamp(_waveform[(int)(Math.Clamp(t, 0f, 1f) * (_waveform.Length - 1))] * Gain, -1f, 1f);

        /// <summary>Úroveň spektra v poloze <paramref name="t"/> (0 = basy, 1 = výšky).</summary>
        public float Band(float t) => _bands[(int)(Math.Clamp(t, 0f, 1f) * (_bands.Length - 1))];
    }
}
