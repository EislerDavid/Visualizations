namespace Visualizations.Core.Audio;

/// <summary>
/// Kruhový buffer posledních vzorků. Zapisuje vlákno zachytávání, čte vlákno vykreslování.
/// </summary>
public sealed class SampleRingBuffer
{
    private readonly float[] _buffer;
    private readonly Lock _lock = new();
    private int _writePosition;

    public SampleRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _buffer = new float[capacity];
    }

    public int Capacity => _buffer.Length;

    public void Write(ReadOnlySpan<float> samples)
    {
        lock (_lock)
        {
            if (samples.Length >= _buffer.Length)
            {
                samples[^_buffer.Length..].CopyTo(_buffer);
                _writePosition = 0;
                return;
            }

            int first = Math.Min(samples.Length, _buffer.Length - _writePosition);
            samples[..first].CopyTo(_buffer.AsSpan(_writePosition));
            samples[first..].CopyTo(_buffer);
            _writePosition = (_writePosition + samples.Length) % _buffer.Length;
        }
    }

    /// <summary>Zkopíruje posledních <c>destination.Length</c> vzorků, od nejstaršího po nejnovější.</summary>
    public void ReadLatest(Span<float> destination)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(destination.Length, _buffer.Length, nameof(destination));

        lock (_lock)
        {
            int start = _writePosition - destination.Length;
            if (start < 0)
            {
                start += _buffer.Length;
            }

            int first = Math.Min(destination.Length, _buffer.Length - start);
            _buffer.AsSpan(start, first).CopyTo(destination);
            _buffer.AsSpan(0, destination.Length - first).CopyTo(destination[first..]);
        }
    }
}
