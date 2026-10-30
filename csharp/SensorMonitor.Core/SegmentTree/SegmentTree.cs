namespace SensorMonitor.Core.SegmentTrees;

/// <summary>
/// A segment tree over an array of numbers. It answers max, min and sum
/// for any range of positions without scanning the whole range.
/// Ranges are inclusive on both ends: (2, 5) means positions 2, 3, 4 and 5.
/// NOT thread-safe: one thread builds and updates it.
/// </summary>
public class SegmentTree
{
    // Each "box" of the tree stores the max, min and sum of its range.
    // Box 1 is the root (the whole array). Box i has children 2*i and 2*i+1.
    private double[] _max = Array.Empty<double>();
    private double[] _min = Array.Empty<double>();
    private double[] _sum = Array.Empty<double>();

    private int _size;  // how many values the tree was built from

    /// <summary>
    /// How many values the tree covers (0 until Build is called).
    /// </summary>
    public int Count => _size;
}