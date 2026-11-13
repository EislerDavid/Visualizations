using System.Numerics;
using Silk.NET.OpenGL;
using Visualizations.App.Rendering;
using Visualizations.Core.Analysis;

namespace Visualizations.App.Effects;

/// <summary>
/// „Scope“ z rodiny Bars and Waves: osciloskop – průběh signálu jako zářící čára
/// s pomalu se měnící barvou. Spouštění (trigger) na průchodu nulou drží periodické tóny na místě.
/// </summary>
internal sealed class ScopeVisualization : IVisualization
{
    private const int PointCount = AudioFrame.WaveformLength;
    private const float AmplitudeScale = 2.5f;
    private const float HeightFraction = 0.42f;

    private readonly Vector2[] _points = new Vector2[PointCount];
    private GlowLine? _line;

    public void Load(GL gl) => _line = new GlowLine(gl);

    public void Resize(int width, int height)
    {
    }

    public void Render(in FrameContext frame)
    {
        if (_line is null)
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

        var color = Colors.Hsv((float)(frame.Time * 0.03 % 1.0), 0.7f, 1f);
        _line.Draw(_points, closed: false, color, new Vector2(width, height));
    }

    public void Dispose() => _line?.Dispose();

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
}
