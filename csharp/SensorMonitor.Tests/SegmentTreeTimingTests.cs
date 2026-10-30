using System.Diagnostics;
using SensorMonitor.Core.SegmentTrees;
using Xunit.Abstractions;

namespace SensorMonitor.Tests;

public class SegmentTreeTimingTests
{
    private readonly ITestOutputHelper _output;

    public SegmentTreeTimingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Timing_TreeVersusPlainScan_SameAnswersAndTimesPrinted()
    {
        const int n = 100_000;
        const int queries = 2_000;

        var random = new Random(1);
        var values = new double[n];
        for (int i = 0; i < n; i++)
            values[i] = random.Next(-1000, 1000);

        // The same list of ranges for both methods
        var lefts = new int[queries];
        var rights = new int[queries];
        for (int q = 0; q < queries; q++)
        {
            int a = random.Next(n);
            int b = random.Next(n);
            lefts[q] = Math.Min(a, b);
            rights[q] = Math.Max(a, b);
        }

        var tree = new SegmentTree();
        var buildWatch = Stopwatch.StartNew();
        tree.Build(values);
        buildWatch.Stop();

        // Tree: sum of the answers to all queries
        double treeTotal = 0;
        var treeWatch = Stopwatch.StartNew();
        for (int q = 0; q < queries; q++)
            treeTotal += tree.QueryMax(lefts[q], rights[q]);
        treeWatch.Stop();

        // Plain scan: same questions, answered by looping over the range
        double scanTotal = 0;
        var scanWatch = Stopwatch.StartNew();
        for (int q = 0; q < queries; q++)
        {
            double max = double.NegativeInfinity;
            for (int i = lefts[q]; i <= rights[q]; i++)
                max = Math.Max(max, values[i]);
            scanTotal += max;
        }
        scanWatch.Stop();

        _output.WriteLine($"Values: {n}, queries: {queries}");
        _output.WriteLine($"Tree build (once): {buildWatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"Tree queries:      {treeWatch.Elapsed.TotalMilliseconds:F2} ms");
        _output.WriteLine($"Plain scan:        {scanWatch.Elapsed.TotalMilliseconds:F2} ms");

        // Both methods must agree. Speed is printed, not asserted.
        Assert.Equal(scanTotal, treeTotal);
    }
}