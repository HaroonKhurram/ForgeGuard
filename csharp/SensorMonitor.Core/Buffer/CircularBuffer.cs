namespace SensorMonitor.Core.Buffer;

/// <summary>
/// A fixed-size circular queue built on a plain array.
/// When full, adding a new item overwrites the oldest one.
/// </summary>
public class CircularBuffer<T>
{
    private readonly T[] _items;   // the fixed array that holds the data
    private int _head;             // array index of the OLDEST item
    private int _count;            // how many items are stored right now
        private readonly object _lock = new();  // protects against MQTT writing while the dashboard reads

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");

        _items = new T[capacity];
    }

    public int Capacity => _items.Length;
}