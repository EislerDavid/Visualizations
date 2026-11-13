using Visualizations.Core.Analysis;

namespace Visualizations.Core.Tests;

public class SpectrumAnalyzerTests
{
    private const int SampleRate = 48000;
    private const float Frame = 1f / 60;

    [Fact]
    public void Silence_LeavesAllBandsEmpty()
    {
        var analyzer = new SpectrumAnalyzer(SampleRate);

        analyzer.Process(new float[analyzer.FftSize], Frame);

        Assert.All(analyzer.Bands.ToArray(), b => Assert.Equal(0f, b));
    }

    [Theory]
    [InlineData(100f)]
    [InlineData(1000f)]
    [InlineData(8000f)]
    public void LoudSine_FillsOnlyBandsAroundItsFrequency(float frequency)
    {
        // Bez sklonu a vyhlazení – testujeme jen, do kterého pásma energie dopadne.
        var analyzer = new SpectrumAnalyzer(SampleRate) { TiltDecibelsPerOctave = 0f, AttackTime = 0f };

        analyzer.Process(Sine(frequency, amplitude: 1f, analyzer.FftSize), Frame);

        var bands = analyzer.Bands.ToArray();
        int loudest = Array.IndexOf(bands, bands.Max());
        Assert.Equal(1f, bands[loudest]);
        Assert.Equal(BandOf(frequency, analyzer.BandCount), loudest, tolerance: 1);

        // Hannovo okno drží energii pohromadě – pásma vzdálená přes dvě oktávy zůstanou prázdná.
        // (V basech je bin FFT (11,7 Hz) širší než pásmo, takže únik do sousedních pásem je tam delší.)
        int octaveInBands = (int)MathF.Ceiling(analyzer.BandCount / MathF.Log2(16000f / 40f));
        for (int b = 0; b < bands.Length; b++)
        {
            if (Math.Abs(b - loudest) > 2 * octaveInBands)
            {
                Assert.True(bands[b] == 0f, $"Pásmo {b} má hodnotu {bands[b]}.");
            }
        }
    }

    [Fact]
    public void Attack_IsSmoothedButReachesTargetWithinFewTimeConstants()
    {
        var analyzer = new SpectrumAnalyzer(SampleRate) { TiltDecibelsPerOctave = 0f, AttackTime = 0.07f };
        int band = BandOf(1000f, analyzer.BandCount);
        var tone = Sine(1000f, 1f, analyzer.FftSize);

        analyzer.Process(tone, Frame);
        float afterOneFrame = analyzer.Bands[band];
        for (int i = 0; i < 29; i++)
        {
            analyzer.Process(tone, Frame);
        }

        Assert.Equal(1f - MathF.Exp(-Frame / 0.07f), afterOneFrame, 1e-4f);
        Assert.True(analyzer.Bands[band] > 0.99f, $"Po 0,5 s je pásmo jen na {analyzer.Bands[band]}.");
    }

    [Fact]
    public void AfterSoundStops_BandsFallLinearlyAndPeaksHold()
    {
        var analyzer = new SpectrumAnalyzer(SampleRate) { TiltDecibelsPerOctave = 0f, AttackTime = 0f };
        int band = BandOf(1000f, analyzer.BandCount);

        analyzer.Process(Sine(1000f, 1f, analyzer.FftSize), 0.1f);
        analyzer.Process(new float[analyzer.FftSize], 0.1f);

        Assert.Equal(1f - analyzer.FallRate * 0.1f, analyzer.Bands[band], 1e-5f);
        Assert.Equal(1f, analyzer.Peaks[band]);
    }

    private static float[] Sine(float frequency, float amplitude, int length)
    {
        var samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            samples[i] = amplitude * MathF.Sin(2 * MathF.PI * frequency * i / SampleRate);
        }

        return samples;
    }

    private static int BandOf(float frequency, int bandCount) =>
        (int)(MathF.Log(frequency / 40f) / MathF.Log(16000f / 40f) * bandCount);
}
