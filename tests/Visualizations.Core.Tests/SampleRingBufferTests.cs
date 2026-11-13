using Visualizations.Core.Audio;

namespace Visualizations.Core.Tests;

public class SampleRingBufferTests
{
    [Fact]
    public void ReadLatest_AfterWrapAround_ReturnsNewestSamplesOldestFirst()
    {
        var buffer = new SampleRingBuffer(8);
        buffer.Write([1, 2, 3, 4, 5]);
        buffer.Write([6, 7, 8, 9, 10, 11]);

        var latest = new float[5];
        buffer.ReadLatest(latest);

        Assert.Equal([7f, 8, 9, 10, 11], latest);
    }

    [Fact]
    public void Write_LongerThanCapacity_KeepsOnlyTail()
    {
        var buffer = new SampleRingBuffer(4);
        buffer.Write([1, 2, 3, 4, 5, 6, 7]);

        var latest = new float[4];
        buffer.ReadLatest(latest);

        Assert.Equal([4f, 5, 6, 7], latest);
    }

    [Fact]
    public void ReadLatest_BeforeAnyWrite_ReturnsSilence()
    {
        var latest = new float[3];
        new SampleRingBuffer(4).ReadLatest(latest);

        Assert.Equal([0f, 0, 0], latest);
    }
}
