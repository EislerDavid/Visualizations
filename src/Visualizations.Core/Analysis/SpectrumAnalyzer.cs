namespace Visualizations.Core.Analysis;

/// <summary>
/// Z posledních vzorků spočítá spektrum rozdělené do logaritmických pásem (0–1)
/// s „analyzátorovým“ chováním: okamžitý náběh, pozvolný pád a padající špičky.
/// </summary>
public sealed class SpectrumAnalyzer
{
    private readonly Fft _fft;
    private readonly float[] _window;
    private readonly float[] _real;
    private readonly float[] _imaginary;
    private readonly float[] _magnitudes;
    private readonly float _magnitudeScale;

    private readonly int[] _bandFirstBin;
    private readonly int[] _bandEndBin;
    private readonly float[] _bandCenterBin;
    private readonly float[] _bandOctavesFrom1k;

    private readonly float[] _bands;
    private readonly float[] _peaks;
    private readonly float[] _peakHold;
    private readonly float[] _peakVelocity;

    public SpectrumAnalyzer(
        int sampleRate,
        int bandCount = 64,
        int fftSize = 4096,
        float minFrequency = 40f,
        float maxFrequency = 16000f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bandCount, 1);
        maxFrequency = Math.Min(maxFrequency, sampleRate / 2f);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minFrequency, maxFrequency);

        _fft = new Fft(fftSize);
        FftSize = fftSize;
        BandCount = bandCount;

        // Periodické Hannovo okno; koherentní zisk 0,5 → plnoškálový sinus má po FFT amplitudu N/4.
        _window = new float[fftSize];
        for (int i = 0; i < fftSize; i++)
        {
            _window[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / fftSize));
        }

        _magnitudeScale = 4f / fftSize;
        _real = new float[fftSize];
        _imaginary = new float[fftSize];
        _magnitudes = new float[fftSize / 2 + 1];

        float binHz = (float)sampleRate / fftSize;
        _bandFirstBin = new int[bandCount];
        _bandEndBin = new int[bandCount];
        _bandCenterBin = new float[bandCount];
        _bandOctavesFrom1k = new float[bandCount];
        float ratio = maxFrequency / minFrequency;
        for (int b = 0; b < bandCount; b++)
        {
            float low = minFrequency * MathF.Pow(ratio, (float)b / bandCount);
            float high = minFrequency * MathF.Pow(ratio, (float)(b + 1) / bandCount);
            float center = MathF.Sqrt(low * high);
            _bandFirstBin[b] = (int)MathF.Ceiling(low / binHz);
            _bandEndBin[b] = Math.Min((int)MathF.Ceiling(high / binHz), _magnitudes.Length);
            _bandCenterBin[b] = center / binHz;
            _bandOctavesFrom1k[b] = MathF.Log2(center / 1000f);
        }

        _bands = new float[bandCount];
        _peaks = new float[bandCount];
        _peakHold = new float[bandCount];
        _peakVelocity = new float[bandCount];
    }

    /// <summary>Počet vzorků, které <see cref="Process"/> očekává.</summary>
    public int FftSize { get; }

    public int BandCount { get; }

    /// <summary>Vyhlazená úroveň pásem, 0–1.</summary>
    public ReadOnlySpan<float> Bands => _bands;

    /// <summary>Padající špičky pásem, 0–1.</summary>
    public ReadOnlySpan<float> Peaks => _peaks;

    /// <summary>Úroveň (dB vůči plné škále), která odpovídá prázdnému pásmu.</summary>
    public float MinDecibels { get; set; } = -70f;

    /// <summary>Úroveň (dB vůči plné škále), která odpovídá plnému pásmu.</summary>
    public float MaxDecibels { get; set; } = -10f;

    /// <summary>Zvýraznění výšek v dB na oktávu vůči 1 kHz (hudba má přirozeně méně energie ve výškách).</summary>
    public float TiltDecibelsPerOctave { get; set; } = 3f;

    /// <summary>Rychlost pádu pásem (podíl plné výšky za sekundu).</summary>
    public float FallRate { get; set; } = 2f;

    /// <summary>Jak dlouho (s) špička vydrží nahoře, než začne padat.</summary>
    public float PeakHoldTime { get; set; } = 0.4f;

    /// <summary>Zrychlení pádu špiček (podíl plné výšky za s²).</summary>
    public float PeakGravity { get; set; } = 3f;

    /// <param name="samples">Posledních <see cref="FftSize"/> mono vzorků (nejstarší první).</param>
    /// <param name="deltaTime">Čas od minulého volání v sekundách.</param>
    public void Process(ReadOnlySpan<float> samples, float deltaTime)
    {
        if (samples.Length != FftSize)
        {
            throw new ArgumentException($"Očekáváno {FftSize} vzorků, přišlo {samples.Length}.", nameof(samples));
        }

        for (int i = 0; i < FftSize; i++)
        {
            _real[i] = samples[i] * _window[i];
            _imaginary[i] = 0f;
        }

        _fft.Forward(_real, _imaginary);

        for (int k = 0; k < _magnitudes.Length; k++)
        {
            _magnitudes[k] = MathF.Sqrt(_real[k] * _real[k] + _imaginary[k] * _imaginary[k]) * _magnitudeScale;
        }

        float range = MaxDecibels - MinDecibels;
        for (int b = 0; b < BandCount; b++)
        {
            float decibels = 20f * MathF.Log10(MathF.Max(BandMagnitude(b), 1e-9f))
                + TiltDecibelsPerOctave * _bandOctavesFrom1k[b];
            float target = Math.Clamp((decibels - MinDecibels) / range, 0f, 1f);
            UpdateBand(b, target, deltaTime);
        }
    }

    private float BandMagnitude(int band)
    {
        int first = _bandFirstBin[band];
        int end = _bandEndBin[band];
        if (first < end)
        {
            float max = 0f;
            for (int k = first; k < end; k++)
            {
                max = MathF.Max(max, _magnitudes[k]);
            }

            return max;
        }

        // Úzká basová pásma neobsahují střed žádného binu – interpolujeme mezi sousedy.
        float position = _bandCenterBin[band];
        int k0 = Math.Min((int)position, _magnitudes.Length - 2);
        float t = position - k0;
        return _magnitudes[k0] + (_magnitudes[k0 + 1] - _magnitudes[k0]) * t;
    }

    private void UpdateBand(int b, float target, float deltaTime)
    {
        _bands[b] = target >= _bands[b]
            ? target
            : MathF.Max(target, _bands[b] - FallRate * deltaTime);

        if (_bands[b] >= _peaks[b])
        {
            _peaks[b] = _bands[b];
            _peakHold[b] = PeakHoldTime;
            _peakVelocity[b] = 0f;
        }
        else if (_peakHold[b] > 0f)
        {
            _peakHold[b] -= deltaTime;
        }
        else
        {
            _peakVelocity[b] += PeakGravity * deltaTime;
            _peaks[b] = MathF.Max(_bands[b], _peaks[b] - _peakVelocity[b] * deltaTime);
        }
    }
}
