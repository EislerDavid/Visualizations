using System.Numerics;

namespace Visualizations.Core.Analysis;

/// <summary>Iterativní radix-2 FFT (in-place) s předpočítanými twiddle faktory.</summary>
public sealed class Fft
{
    private readonly int[] _bitReverse;
    private readonly float[] _cos;
    private readonly float[] _sin;

    public Fft(int size)
    {
        if (size < 2 || !BitOperations.IsPow2(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Velikost FFT musí být mocnina dvou ≥ 2.");
        }

        Size = size;
        int bits = BitOperations.Log2((uint)size);

        _bitReverse = new int[size];
        for (int i = 0; i < size; i++)
        {
            int reversed = 0;
            for (int b = 0; b < bits; b++)
            {
                reversed |= ((i >> b) & 1) << (bits - 1 - b);
            }

            _bitReverse[i] = reversed;
        }

        // Dopředná transformace: W^k = e^(-2πik/N).
        _cos = new float[size / 2];
        _sin = new float[size / 2];
        for (int k = 0; k < size / 2; k++)
        {
            double angle = 2 * Math.PI * k / size;
            _cos[k] = (float)Math.Cos(angle);
            _sin[k] = (float)-Math.Sin(angle);
        }
    }

    public int Size { get; }

    /// <summary>Dopředná FFT; reálná a imaginární část se přepíší výsledkem.</summary>
    public void Forward(Span<float> real, Span<float> imaginary)
    {
        if (real.Length != Size || imaginary.Length != Size)
        {
            throw new ArgumentException($"Vstupy musí mít délku {Size}.");
        }

        for (int i = 0; i < Size; i++)
        {
            int j = _bitReverse[i];
            if (j > i)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for (int length = 2; length <= Size; length <<= 1)
        {
            int half = length >> 1;
            int step = Size / length;
            for (int start = 0; start < Size; start += length)
            {
                for (int k = 0; k < half; k++)
                {
                    float wr = _cos[k * step];
                    float wi = _sin[k * step];
                    int a = start + k;
                    int b = a + half;

                    float xr = real[b] * wr - imaginary[b] * wi;
                    float xi = real[b] * wi + imaginary[b] * wr;
                    real[b] = real[a] - xr;
                    imaginary[b] = imaginary[a] - xi;
                    real[a] += xr;
                    imaginary[a] += xi;
                }
            }
        }
    }
}
