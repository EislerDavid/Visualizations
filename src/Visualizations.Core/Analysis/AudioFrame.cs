using Visualizations.Core.Audio;

namespace Visualizations.Core.Analysis;

/// <summary>
/// Vše, co vizualizace o zvuku potřebují pro jeden snímek: spektrum, nedávné vzorky a hlasitost.
/// Aktualizuje se jednou za snímek a sdílí se mezi všemi běžícími vizualizacemi.
/// </summary>
public sealed class AudioFrame
{
    /// <summary>Délka průběhu signálu, se kterou počítají vizualizace typu osciloskop (~21 ms při 48 kHz).</summary>
    public const int WaveformLength = 1024;

    private readonly float[] _samples;

    public AudioFrame(int sampleRate)
    {
        SampleRate = sampleRate;
        Spectrum = new SpectrumAnalyzer(sampleRate);
        _samples = new float[Math.Max(Spectrum.FftSize, 2 * WaveformLength)];
    }

    public int SampleRate { get; }

    public SpectrumAnalyzer Spectrum { get; }

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

    /// <summary>Lineární zesílení odpovídající nastavené citlivosti (pro vizualizace pracující přímo se vzorky).</summary>
    public float Gain => MathF.Pow(10f, Spectrum.GainDecibels / 20f);

    public void Update(AudioCapture capture, float deltaTime)
    {
        capture.ReadLatest(_samples);
        Spectrum.Process(_samples.AsSpan(_samples.Length - Spectrum.FftSize), deltaTime);

        float sum = 0f;
        foreach (float sample in Waveform)
        {
            sum += sample * sample;
        }

        Level = MathF.Sqrt(sum / WaveformLength);
    }
}
