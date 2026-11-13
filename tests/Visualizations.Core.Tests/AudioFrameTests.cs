using Visualizations.Core.Analysis;

namespace Visualizations.Core.Tests;

public class AudioFrameTests
{
    private const int SampleRate = 48000;
    private const float Frame = 1f / 60;

    [Fact]
    public void BassTone_RaisesBassButNotTreble()
    {
        var audio = new AudioFrame(SampleRate);
        var tone = Sine(60f, 0.5f, audio.SampleCount);

        for (int i = 0; i < 30; i++)
        {
            audio.Update(tone, Frame);
        }

        Assert.True(audio.Bass > 0.1f, $"Basy {audio.Bass}");
        Assert.True(audio.Treble < 0.01f, $"Výšky {audio.Treble}");
        Assert.True(audio.Bass > audio.Mid, $"Basy {audio.Bass}, středy {audio.Mid}");
    }

    [Fact]
    public void KickAfterSilence_IsDetectedAsBeat_SteadyToneIsNot()
    {
        var audio = new AudioFrame(SampleRate);
        var silence = new float[audio.SampleCount];
        var tone = Sine(60f, 0.5f, audio.SampleCount);

        for (int i = 0; i < 60; i++)
        {
            audio.Update(silence, Frame);
        }

        Assert.Equal(0f, audio.Beat);

        float strongest = 0f;
        for (int i = 0; i < 12; i++)
        {
            audio.Update(tone, Frame);
            strongest = MathF.Max(strongest, audio.Beat);
        }

        Assert.Equal(1f, strongest);

        // Ustálený tón: klouzavý průměr ho dožene a další údery už nepřijdou.
        for (int i = 0; i < 120; i++)
        {
            audio.Update(tone, Frame);
        }

        float laterStrongest = 0f;
        for (int i = 0; i < 60; i++)
        {
            audio.Update(tone, Frame);
            laterStrongest = MathF.Max(laterStrongest, audio.Beat);
        }

        Assert.Equal(0f, laterStrongest);
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
}
