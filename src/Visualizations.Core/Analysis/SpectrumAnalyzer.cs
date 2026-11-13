namespace Visualizations.Core.Analysis;

/// <summary>
/// Z posledních vzorků spočítá energii v logaritmických pásmech (0–1)
/// s „analyzátorovým“ chováním: vyhlazený náběh, pozvolný pád a padající špičky.
/// </summary>
public sealed class SpectrumAnalyzer
{
    // Ekvivalentní šumová šířka Hannova okna v binech – sinus se rozprostře do ~1,5 binu energie.
    private const float HannNoiseBandwidth = 1.5f;

    private readonly Fft _fft;
    private readonly float[] _window;
    private readonly float[] _real;
    private readonly float[] _imaginary;
    private readonly float[] _power;
    private readonly float _powerScale;

    private readonly int[] _bandFirstBin;
    private readonly int[] _bandEndBin;
    private readonly float[] _bandCenterBin;
    private readonly float[] _bandWidthInBins;
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

        // Periodické Hannovo okno. Sinus s amplitudou A má po FFT v nejvyšším binu |X| = A·N/4;
        // škálování je zvolené tak, aby součet energie přes jeho biny dal A² (plnoškálový sinus = 0 dB).
        _window = new float[fftSize];
        for (int i = 0; i < fftSize; i++)
        {
            _window[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / fftSize));
        }

        float magnitudeScale = 4f / fftSize;
        _powerScale = magnitudeScale * magnitudeScale / HannNoiseBandwidth;
        _real = new float[fftSize];
        _imaginary = new float[fftSize];
        _power = new float[fftSize / 2 + 1];

        float binHz = (float)sampleRate / fftSize;
        _bandFirstBin = new int[bandCount];
        _bandEndBin = new int[bandCount];
        _bandCenterBin = new float[bandCount];
        _bandWidthInBins = new float[bandCount];
        _bandOctavesFrom1k = new float[bandCount];
        float ratio = maxFrequency / minFrequency;
        for (int b = 0; b < bandCount; b++)
        {
            float low = minFrequency * MathF.Pow(ratio, (float)b / bandCount);
            float high = minFrequency * MathF.Pow(ratio, (float)(b + 1) / bandCount);
            float center = MathF.Sqrt(low * high);
            _bandFirstBin[b] = (int)MathF.Ceiling(low / binHz);
            _bandEndBin[b] = Math.Min((int)MathF.Ceiling(high / binHz), _power.Length);
            _bandCenterBin[b] = center / binHz;
            _bandWidthInBins[b] = (high - low) / binHz;
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

    // Výchozí hodnoty jsou naladěné na nahrávce hudby ze Spotify (normalizace hlasitosti zapnutá):
    // sloupce v průměru do poloviny výšky, třepotání mezi snímky ~40 % původní verze.

    /// <summary>Energie pásma (dB vůči plnoškálovému sinu), která odpovídá prázdnému sloupci.</summary>
    public float MinDecibels { get; set; } = -62f;

    /// <summary>Energie pásma (dB vůči plnoškálovému sinu), která odpovídá plnému sloupci.</summary>
    public float MaxDecibels { get; set; } = -12f;

    /// <summary>Citlivost: o kolik dB se změřená energie zesílí (kladné = vyšší sloupce).</summary>
    public float GainDecibels { get; set; }

    /// <summary>Zvýraznění výšek v dB na oktávu vůči 1 kHz (hudba má ve výškách méně energie).</summary>
    public float TiltDecibelsPerOctave { get; set; } = 2f;

    /// <summary>Časová konstanta náběhu v sekundách; 0 = okamžitý náběh.</summary>
    public float AttackTime { get; set; } = 0.07f;

    /// <summary>Rychlost pádu pásem (podíl plné výšky za sekundu).</summary>
    public float FallRate { get; set; } = 0.8f;

    /// <summary>Jak dlouho (s) špička vydrží nahoře, než začne padat.</summary>
    public float PeakHoldTime { get; set; } = 0.4f;

    /// <summary>Zrychlení pádu špiček (podíl plné výšky za s²).</summary>
    public float PeakGravity { get; set; } = 2f;

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

        for (int k = 0; k < _power.Length; k++)
        {
            _power[k] = (_real[k] * _real[k] + _imaginary[k] * _imaginary[k]) * _powerScale;
        }

        float range = MaxDecibels - MinDecibels;
        float attack = AttackTime > 0f ? 1f - MathF.Exp(-deltaTime / AttackTime) : 1f;
        for (int b = 0; b < BandCount; b++)
        {
            float decibels = 10f * MathF.Log10(MathF.Max(BandPower(b), 1e-12f))
                + TiltDecibelsPerOctave * _bandOctavesFrom1k[b]
                + GainDecibels;
            float target = Math.Clamp((decibels - MinDecibels) / range, 0f, 1f);
            UpdateBand(b, target, attack, deltaTime);
        }
    }

    /// <summary>Celková energie pásma (součet přes jeho biny).</summary>
    private float BandPower(int band)
    {
        int first = _bandFirstBin[band];
        int end = _bandEndBin[band];
        if (first < end)
        {
            float sum = 0f;
            for (int k = first; k < end; k++)
            {
                sum += _power[k];
            }

            return sum;
        }

        // Úzká basová pásma neobsahují střed žádného binu – vezmeme interpolovanou hustotu
        // energie v jejich středu a vynásobíme šířkou pásma (v binech).
        float position = _bandCenterBin[band];
        int k0 = Math.Min((int)position, _power.Length - 2);
        float t = position - k0;
        float density = _power[k0] + (_power[k0 + 1] - _power[k0]) * t;
        return density * _bandWidthInBins[band];
    }

    private void UpdateBand(int b, float target, float attack, float deltaTime)
    {
        _bands[b] = target > _bands[b]
            ? _bands[b] + (target - _bands[b]) * attack
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
