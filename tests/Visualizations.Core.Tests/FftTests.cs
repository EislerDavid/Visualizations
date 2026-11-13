using Visualizations.Core.Analysis;

namespace Visualizations.Core.Tests;

public class FftTests
{
    [Theory]
    [InlineData(64, 5)]
    [InlineData(1024, 37)]
    public void SineAtBinCenter_ProducesSinglePeakWithAmplitudeHalfN(int size, int bin)
    {
        var real = new float[size];
        var imaginary = new float[size];
        for (int i = 0; i < size; i++)
        {
            real[i] = MathF.Sin(2 * MathF.PI * bin * i / size);
        }

        new Fft(size).Forward(real, imaginary);

        for (int k = 0; k <= size / 2; k++)
        {
            float magnitude = MathF.Sqrt(real[k] * real[k] + imaginary[k] * imaginary[k]);
            float expected = k == bin ? size / 2f : 0f;
            Assert.Equal(expected, magnitude, size * 1e-4f);
        }
    }

    [Fact]
    public void Impulse_ProducesFlatSpectrum()
    {
        const int size = 128;
        var real = new float[size];
        var imaginary = new float[size];
        real[0] = 1f;

        new Fft(size).Forward(real, imaginary);

        Assert.All(real, r => Assert.Equal(1f, r, 1e-5f));
        Assert.All(imaginary, im => Assert.Equal(0f, im, 1e-5f));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void NonPowerOfTwoSize_Throws(int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fft(size));
}
