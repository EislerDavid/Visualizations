using Visualizations.Core.Audio;

namespace Visualizations.Core.Analysis;

/// <summary>
/// Vše, co vizualizace o zvuku potřebují pro jeden snímek: spektrum, nedávné vzorky, hlasitost,
/// úrovně basů / středů / výšek a údery. Aktualizuje se jednou za snímek a sdílí se mezi všemi
/// běžícími vizualizacemi.
/// </summary>
public sealed class AudioFrame
{
    /// <summary>Délka průběhu signálu, se kterou počítají vizualizace typu osciloskop (~21 ms při 48 kHz).</summary>
    public const int WaveformLength = 1024;

    private const float BassMaxHz = 150f;
    private const float MidMaxHz = 2000f;

    private readonly float[] _samples;
    private readonly BeatDetector _beatDetector;
    private readonly int _midStartBand;
    private readonly int _trebleStartBand;

    public AudioFrame(int sampleRate)
    {
        SampleRate = sampleRate;
        Spectrum = new SpectrumAnalyzer(sampleRate);
        _beatDetector = new BeatDetector(sampleRate, BassMaxHz);
        _samples = new float[Math.Max(Spectrum.FftSize, 2 * WaveformLength)];

        _midStartBand = FirstBandAbove(BassMaxHz);
        _trebleStartBand = FirstBandAbove(MidMaxHz);
    }

    public int SampleRate { get; }

    public SpectrumAnalyzer Spectrum { get; }

    /// <summary>Kolik vzorků očekává <see cref="Update(ReadOnlySpan{float}, float)"/>.</summary>
    public int SampleCount => _samples.Length;

    /// <summary>Vyhlazená úroveň pásem spektra, 0–1.</summary>
    public ReadOnlySpan<float> Bands => Spectrum.Bands;

    /// <summary>Padající špičky pásem spektra, 0–1.</summary>
    public ReadOnlySpan<float> Peaks => Spectrum.Peaks;

    /// <summary>Nedávné mono vzorky (−1..1), nejstarší první; nejnovější je poslední.</summary>
    public ReadOnlySpan<float> RecentSamples => _samples;

    /// <summary>Posledních <see cref="WaveformLength"/> vzorků.</summary>
    public ReadOnlySpan<float> Waveform => _samples.AsSpan(_samples.Length - WaveformLength);

    /// <summary>Hlasitost (RMS) průběhu <see cref="Waveform"/>, lineárně.</summary>
    public float Level { get; private set; }

    /// <summary>Průměrná úroveň pásem pod 150 Hz, 0–1.</summary>
    public float Bass { get; private set; }

    /// <summary>Průměrná úroveň pásem 150 Hz – 2 kHz, 0–1.</summary>
    public float Mid { get; private set; }

    /// <summary>Průměrná úroveň pásem nad 2 kHz, 0–1.</summary>
    public float Treble { get; private set; }

    /// <summary>Úder (kopák): v okamžiku úderu 1, pak během ~0,2 s klesne na 0.</summary>
    public float Beat => _beatDetector.Beat;

    public BeatDetector BeatDetector => _beatDetector;

    /// <summary>Lineární zesílení odpovídající nastavené citlivosti (pro vizualizace pracující přímo se vzorky).</summary>
    public float Gain => MathF.Pow(10f, Spectrum.GainDecibels / 20f);

    public void Update(AudioCapture capture, float deltaTime)
    {
        capture.ReadLatest(_samples);
        Analyze(deltaTime);
    }

    /// <param name="latestSamples">Posledních <see cref="SampleCount"/> vzorků, nejstarší první.</param>
    public void Update(ReadOnlySpan<float> latestSamples, float deltaTime)
    {
        if (latestSamples.Length != _samples.Length)
        {
            throw new ArgumentException($"Očekáváno {_samples.Length} vzorků, přišlo {latestSamples.Length}.", nameof(latestSamples));
        }

        latestSamples.CopyTo(_samples);
        Analyze(deltaTime);
    }

    private void Analyze(float deltaTime)
    {
        Spectrum.Process(_samples.AsSpan(_samples.Length - Spectrum.FftSize), deltaTime);

        float sum = 0f;
        foreach (float sample in Waveform)
        {
            sum += sample * sample;
        }

        Level = MathF.Sqrt(sum / WaveformLength);

        var bands = Spectrum.Bands;
        Bass = Average(bands[.._midStartBand]);
        Mid = Average(bands[_midStartBand.._trebleStartBand]);
        Treble = Average(bands[_trebleStartBand..]);

        _beatDetector.Process(_samples, deltaTime);
    }

    private int FirstBandAbove(float frequency)
    {
        int band = 0;
        while (band < Spectrum.BandCount && Spectrum.BandCenterFrequency(band) < frequency)
        {
            band++;
        }

        return Math.Clamp(band, 1, Spectrum.BandCount - 1);
    }

    private static float Average(ReadOnlySpan<float> values)
    {
        if (values.IsEmpty)
        {
            return 0f;
        }

        float sum = 0f;
        foreach (float value in values)
        {
            sum += value;
        }

        return sum / values.Length;
    }
}
