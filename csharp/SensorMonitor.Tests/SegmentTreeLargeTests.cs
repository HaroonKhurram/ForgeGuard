using SensorMonitor.Core.SegmentTrees;

namespace SensorMonitor.Tests;

public class SegmentTreeLargeTests
{
    private static double[] RandomValues(Random random, int n)
    {
        var values = new double[n];
        for (int i = 0; i < n; i++)
            values[i] = random.Next(-1000, 1000);
        return values;
    }

    // Compares the tree's answers for one range with a plain loop.
    private static void AssertRangeMatches(double[] values, SegmentTree tree, int l, int r)
    {
        double sum = 0;
        double max = double.NegativeInfinity;
        double min = double.PositiveInfinity;
        for (int i = l; i <= r; i++)
        {
            sum += values[i];
            max = Math.Max(max, values[i]);
            min = Math.Min(min, values[i]);
        }

        Assert.Equal(sum, tree.QuerySum(l, r));
        Assert.Equal(max, tree.QueryMax(l, r));
        Assert.Equal(min, tree.QueryMin(l, r));
    }

    [Fact]
    public void LargeArray_RandomQueries_MatchPlainScan()
    {
        var random = new Random(2026); // fixed seed: same numbers every run
        var values = RandomValues(random, 10_007);
        var tree = new SegmentTree();
        tree.Build(values);

        for (int q = 0; q < 5_000; q++)
        {
            int a = random.Next(values.Length);
            int b = random.Next(values.Length);
            AssertRangeMatches(values, tree, Math.Min(a, b), Math.Max(a, b));
        }
    }

    [Fact]
    public void LargeArray_UpdatesMixedWithQueries_MatchPlainScan()
    {
        var random = new Random(77);
        var values = RandomValues(random, 10_007);
        var tree = new SegmentTree();
        tree.Build(values);

        for (int step = 0; step < 2_000; step++)
        {
            int index = random.Next(values.Length);
            double value = random.Next(-1000, 1000);
            values[index] = value;
            tree.Update(index, value);

            // After every update, check 3 random ranges
            for (int q = 0; q < 3; q++)
            {
                int a = random.Next(values.Length);
                int b = random.Next(values.Length);
                AssertRangeMatches(values, tree, Math.Min(a, b), Math.Max(a, b));
            }
        }
    }
}