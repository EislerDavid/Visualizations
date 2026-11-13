namespace Visualizations.Core.Analysis;

/// <summary>
/// Detekce úderů (kopáků). Pracuje se surovým signálem, ne s vyhlazeným spektrem: energie pod ~150 Hz
/// za posledních ~21 ms (v dB) se porovnává s jejím klouzavým průměrem. Úder je skok nad průměr;
/// hystereze zajistí, že jeden kopák spustí jen jeden úder.
/// </summary>
public sealed class BeatDetector
{
    private const int FilterLength = 2048;
    private const int MeasureLength = 1024;

    private readonly float _b0;
    private readonly float _b1;
    private readonly float _b2;
    private readonly float _a1;
    private readonly float _a2;

    private float _average = float.NaN;
    private float _sinceBeat = float.MaxValue;
    private bool _armed = true;

    public BeatDetector(int sampleRate, float cutoffHz = 150f)
    {
        // Butterworthova dolní propust 2. řádu (RBJ Audio EQ Cookbook).
        double w0 = 2 * Math.PI * cutoffHz / sampleRate;
        double alpha = Math.Sin(w0) / (2 * Math.Sqrt(0.5));
        double cos = Math.Cos(w0);
        double a0 = 1 + alpha;
        _b0 = (float)((1 - cos) / 2 / a0);
        _b1 = (float)((1 - cos) / a0);
        _b2 = _b0;
        _a1 = (float)(-2 * cos / a0);
        _a2 = (float)((1 - alpha) / a0);
    }

    /// <summary>O kolik dB musí basová energie vyskočit nad průměr, aby šlo o úder.</summary>
    public float ThresholdDecibels { get; set; } = 5f;

    /// <summary>Pod touto energií (dB vůči plné škále) se údery nehledají – ticho a šum.</summary>
    public float FloorDecibels { get; set; } = -45f;

    /// <summary>Časová konstanta klouzavého průměru energie v sekundách.</summary>
    public float AverageTime { get; set; } = 0.4f;

    /// <summary>Nejkratší rozestup dvou úderů v sekundách.</summary>
    public float MinimumInterval { get; set; } = 0.15f;

    /// <summary>Za jak dlouho (s) pulz úderu klesne z 1 na 0.</summary>
    public float DecayTime { get; set; } = 0.18f;

    /// <summary>Pulz úderu: v okamžiku úderu 1, pak lineárně klesá k 0.</summary>
    public float Beat { get; private set; }

    /// <summary>Aktuální basová energie v dB vůči plné škále.</summary>
    public float EnergyDecibels { get; private set; }

    /// <param name="recentSamples">Nedávné vzorky, nejnovější poslední; použije se jich nejvýš 2048.</param>
    public void Process(ReadOnlySpan<float> recentSamples, float deltaTime)
    {
        var x = recentSamples[^Math.Min(FilterLength, recentSamples.Length)..];
        int measureStart = Math.Max(0, x.Length - MeasureLength);

        float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;
        double sum = 0;
        for (int i = 0; i < x.Length; i++)
        {
            float y = _b0 * x[i] + _b1 * x1 + _b2 * x2 - _a1 * y1 - _a2 * y2;
            x2 = x1;
            x1 = x[i];
            y2 = y1;
            y1 = y;
            if (i >= measureStart)
            {
                sum += y * y;
            }
        }

        EnergyDecibels = 10f * MathF.Log10(MathF.Max((float)(sum / (x.Length - measureStart)), 1e-12f));
        if (float.IsNaN(_average))
        {
            _average = EnergyDecibels;
        }

        float onset = EnergyDecibels - _average;
        _sinceBeat += deltaTime;
        if (onset < ThresholdDecibels / 2f)
        {
            _armed = true;
        }

        if (_armed && onset > ThresholdDecibels && EnergyDecibels > FloorDecibels && _sinceBeat > MinimumInterval)
        {
            Beat = 1f;
            _sinceBeat = 0f;
            _armed = false;
        }
        else
        {
            Beat = MathF.Max(0f, Beat - deltaTime / DecayTime);
        }

        _average += (EnergyDecibels - _average) * (1f - MathF.Exp(-deltaTime / AverageTime));
    }
}
