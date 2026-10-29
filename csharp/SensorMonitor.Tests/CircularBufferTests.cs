using SensorMonitor.Core.Buffer;

namespace SensorMonitor.Tests;

public class CircularBufferTests
{
    [Fact]
    public void NewBuffer_IsEmpty()
    {
        var buffer = new CircularBuffer<int>(3);

        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.ToArrayOldestFirst());
    }

    [Fact]
    public void Add_BeforeFull_KeepsInsertionOrder()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);

        Assert.Equal(2, buffer.Count);
        Assert.Equal(new[] { 1, 2 }, buffer.ToArrayOldestFirst());
    }

    [Fact]
    public void Add_WhenFull_OverwritesOldest()
    {
        var buffer = new CircularBuffer<int>(3);

        // This is our 3-seat example: A,B,C then D,E
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4);
        buffer.Add(5);

        Assert.Equal(3, buffer.Count);
        Assert.Equal(new[] { 3, 4, 5 }, buffer.ToArrayOldestFirst());
    }
}