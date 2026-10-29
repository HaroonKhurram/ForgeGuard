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

    [Fact]
    public void CapacityOne_AlwaysKeepsOnlyTheNewest()
    {
        var buffer = new CircularBuffer<int>(1);

        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        Assert.Equal(1, buffer.Count);
        Assert.Equal(new[] { 30 }, buffer.ToArrayOldestFirst());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_InvalidCapacity_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(capacity));
    }

    [Fact]
    public void ManyWraps_KeepsTheLastNItemsInOrder()
    {
        var buffer = new CircularBuffer<int>(4);

        // 1000 items into 4 seats: the circle is gone around 250 times
        for (int i = 1; i <= 1000; i++)
            buffer.Add(i);

        Assert.Equal(4, buffer.Count);
        Assert.Equal(new[] { 997, 998, 999, 1000 }, buffer.ToArrayOldestFirst());
    }
}