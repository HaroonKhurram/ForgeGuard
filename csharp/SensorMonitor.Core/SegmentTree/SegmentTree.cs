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

    /// <summary>Max of the whole array (the root box).</summary>
    public double RootMax => _max[1];

    /// <summary>Min of the whole array (the root box).</summary>
    public double RootMin => _min[1];

    /// <summary>Sum of the whole array (the root box).</summary>
    public double RootSum => _sum[1];

    /// <summary>
    /// Plays the whole "tournament" once: fills every box from the values.
    /// </summary>
    public void Build(double[] values)
    {
        _size = values.Length;

        // 4 * n slots is the safe, standard size for this layout.
        _max = new double[4 * _size];
        _min = new double[4 * _size];
        _sum = new double[4 * _size];

        BuildBox(values, 1, 0, _size - 1);
    }

    // Fills one box (covering positions left..right), children first.
    private void BuildBox(double[] values, int box, int left, int right)
    {
        if (left == right)
        {
            // A leaf: a single player, so the box just copies the value.
            _max[box] = values[left];
            _min[box] = values[left];
            _sum[box] = values[left];
            return;
        }

        int mid = (left + right) / 2;
        BuildBox(values, 2 * box, left, mid);          // left child
        BuildBox(values, 2 * box + 1, mid + 1, right); // right child

        // Combine the two children.
        _max[box] = Math.Max(_max[2 * box], _max[2 * box + 1]);
        _min[box] = Math.Min(_min[2 * box], _min[2 * box + 1]);
        _sum[box] = _sum[2 * box] + _sum[2 * box + 1];
    }

    /// <summary>
    /// Changes the value at one position and redoes only the boxes above it.
    /// </summary>
    public void Update(int index, double value) => UpdateBox(1, 0, _size - 1, index, value);

    // Walk down to the leaf for 'index', change it, then fix each box on the way back up.
    private void UpdateBox(int box, int left, int right, int index, double value)
    {
        if (left == right)
        {
            // The leaf for this position: store the new value.
            _max[box] = value;
            _min[box] = value;
            _sum[box] = value;
            return;
        }

        int mid = (left + right) / 2;
        if (index <= mid)
            UpdateBox(2 * box, left, mid, index, value);          // position is in the left half
        else
            UpdateBox(2 * box + 1, mid + 1, right, index, value); // position is in the right half

        // Back on the way up: recompute this box from its two children.
        _max[box] = Math.Max(_max[2 * box], _max[2 * box + 1]);
        _min[box] = Math.Min(_min[2 * box], _min[2 * box + 1]);
        _sum[box] = _sum[2 * box] + _sum[2 * box + 1];
    }

    /// <summary>Sum of positions l..r (both included).</summary>
    public double QuerySum(int l, int r) => QuerySumBox(1, 0, _size - 1, l, r);

    /// <summary>Max of positions l..r (both included).</summary>
    public double QueryMax(int l, int r) => QueryMaxBox(1, 0, _size - 1, l, r);

    /// <summary>Min of positions l..r (both included).</summary>
    public double QueryMin(int l, int r) => QueryMinBox(1, 0, _size - 1, l, r);

    // box covers positions left..right; ql..qr is the range being asked for.
    private double QuerySumBox(int box, int left, int right, int ql, int qr)
    {
        if (qr < left || right < ql)
            return 0;                       // no overlap: adds nothing to a sum
        if (ql <= left && right <= qr)
            return _sum[box];               // fully inside: use the ready-made total

        int mid = (left + right) / 2;       // partial overlap: ask both halves
        return QuerySumBox(2 * box, left, mid, ql, qr)
             + QuerySumBox(2 * box + 1, mid + 1, right, ql, qr);
    }

    private double QueryMaxBox(int box, int left, int right, int ql, int qr)
    {
        if (qr < left || right < ql)
            return double.NegativeInfinity; // no overlap: loses every comparison
        if (ql <= left && right <= qr)
            return _max[box];

        int mid = (left + right) / 2;
        return Math.Max(QueryMaxBox(2 * box, left, mid, ql, qr),
                        QueryMaxBox(2 * box + 1, mid + 1, right, ql, qr));
    }

    private double QueryMinBox(int box, int left, int right, int ql, int qr)
    {
        if (qr < left || right < ql)
            return double.PositiveInfinity; // no overlap: loses every comparison
        if (ql <= left && right <= qr)
            return _min[box];

        int mid = (left + right) / 2;
        return Math.Min(QueryMinBox(2 * box, left, mid, ql, qr),
                        QueryMinBox(2 * box + 1, mid + 1, right, ql, qr));
    }
}