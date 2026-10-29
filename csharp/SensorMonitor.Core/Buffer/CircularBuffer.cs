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
    /// <summary>
    /// Adds an item. If the buffer is full, the oldest item is overwritten.
    /// </summary>
    public void Add(T item)
    {
        lock (_lock)
        {
            if (_count < _items.Length)
            {
                // Not full: write in the next free seat.
                _items[(_head + _count) % _items.Length] = item;
                _count++;
            }
            else
            {
                // Full: overwrite the oldest item, then move the head forward.
                _items[_head] = item;
                _head = (_head + 1) % _items.Length;
            }
        }
    }
}