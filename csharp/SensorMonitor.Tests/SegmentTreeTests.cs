using SensorMonitor.Core.SegmentTrees;

namespace SensorMonitor.Tests;

public class SegmentTreeTests
{
    [Fact]
    public void Build_FourValues_RootHoldsMaxMinSum()
    {
        var tree = new SegmentTree();

        // Our worked example: [5, 2, 8, 1]
        tree.Build(new double[] { 5, 2, 8, 1 });

        Assert.Equal(4, tree.Count);
        Assert.Equal(8, tree.RootMax);
        Assert.Equal(1, tree.RootMin);
        Assert.Equal(16, tree.RootSum);
    }

    [Fact]
    public void Build_OddSize_RootHoldsMaxMinSum()
    {
        var tree = new SegmentTree();

        // 5 values split unevenly, which is where bugs like to hide
        tree.Build(new double[] { 3, 1, 4, 1, 5 });

        Assert.Equal(5, tree.Count);
        Assert.Equal(5, tree.RootMax);
        Assert.Equal(1, tree.RootMin);
        Assert.Equal(14, tree.RootSum);
    }

    [Fact]
    public void QuerySum_WorkedExamples()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });

        Assert.Equal(10, tree.QuerySum(1, 2)); // 2 + 8
        Assert.Equal(7, tree.QuerySum(0, 1));  // 5 + 2
        Assert.Equal(16, tree.QuerySum(0, 3)); // everything
    }

    [Fact]
    public void QueryMax_WorkedExamples()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });

        Assert.Equal(8, tree.QueryMax(1, 2));
        Assert.Equal(5, tree.QueryMax(0, 1));
        Assert.Equal(8, tree.QueryMax(0, 3));
    }

    [Fact]
    public void QueryMin_WorkedExamples()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });

        Assert.Equal(2, tree.QueryMin(1, 2));
        Assert.Equal(2, tree.QueryMin(0, 1));
        Assert.Equal(1, tree.QueryMin(0, 3));
    }

    [Fact]
    public void Queries_SinglePosition_ReturnThatValue()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });

        Assert.Equal(8, tree.QuerySum(2, 2));
        Assert.Equal(8, tree.QueryMax(2, 2));
        Assert.Equal(8, tree.QueryMin(2, 2));
    }

    [Fact]
    public void Queries_EveryPossibleRange_MatchPlainScan()
    {
        double[] values = { 3, 1, 4, 1, 5, 9, 2, 6 };
        var tree = new SegmentTree();
        tree.Build(values);

        AssertAllRangesMatch(values, tree);
    }

    [Fact]
    public void Update_WorkedExample_ChangesOnlyWhatItShould()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });

        // Our worked example: position 2 changes from 8 to 3 -> [5, 2, 3, 1]
        tree.Update(2, 3);

        Assert.Equal(4, tree.Count);
        Assert.Equal(5, tree.RootMax);
        Assert.Equal(1, tree.RootMin);
        Assert.Equal(11, tree.RootSum);
        Assert.Equal(4, tree.QuerySum(2, 3));
        Assert.Equal(3, tree.QueryMax(2, 3));
        Assert.Equal(7, tree.QuerySum(0, 1)); // the untouched half is unchanged
    }

    [Fact]
    public void Update_ReplacingTheMax_FindsTheNewMax()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });

        // The old max (8) is replaced by 0 -> [5, 2, 0, 1]
        tree.Update(2, 0);

        Assert.Equal(5, tree.RootMax);   // the new max comes from a different box
        Assert.Equal(0, tree.RootMin);
        Assert.Equal(8, tree.RootSum);
        Assert.Equal(1, tree.QueryMax(2, 3));
        Assert.Equal(0, tree.QueryMin(0, 3));
    }

    [Fact]
    public void Update_FirstAndLastPosition_Work()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });

        // -> [10, 2, 8, -4]
        tree.Update(0, 10);
        tree.Update(3, -4);

        Assert.Equal(10, tree.RootMax);
        Assert.Equal(-4, tree.RootMin);
        Assert.Equal(16, tree.RootSum);
        Assert.Equal(8, tree.QueryMax(1, 3));
        Assert.Equal(-4, tree.QueryMin(0, 3));
    }

    [Fact]
    public void Update_ThenEveryPossibleRange_MatchPlainScan()
    {
        double[] values = { 3, 1, 4, 1, 5, 9, 2, 6 };
        var tree = new SegmentTree();
        tree.Build(values);

        // Change values in the tree AND in the plain array, then compare everything
        int[] positions = { 0, 4, 7, 3 };
        double[] newValues = { 7, -2, 0, 11 };
        for (int i = 0; i < positions.Length; i++)
        {
            values[positions[i]] = newValues[i];
            tree.Update(positions[i], newValues[i]);
        }

        AssertAllRangesMatch(values, tree);
    }

    [Fact]
    public void Update_ManyRandomUpdates_MatchPlainScan()
    {
        var random = new Random(12345); // fixed seed, so the test is repeatable
        var values = new double[50];
        for (int i = 0; i < values.Length; i++)
            values[i] = random.Next(-100, 100);

        var tree = new SegmentTree();
        tree.Build(values);

        for (int step = 0; step < 500; step++)
        {
            int index = random.Next(values.Length);
            double value = random.Next(-100, 100);
            values[index] = value;
            tree.Update(index, value);

            // After every update, check 20 random ranges against a plain loop
            for (int q = 0; q < 20; q++)
            {
                int a = random.Next(values.Length);
                int b = random.Next(values.Length);
                int l = Math.Min(a, b);
                int r = Math.Max(a, b);

                AssertRangeMatches(values, tree, l, r);
            }
        }
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

    // Tries every range l..r of the array.
    private static void AssertAllRangesMatch(double[] values, SegmentTree tree)
    {
        for (int l = 0; l < values.Length; l++)
            for (int r = l; r < values.Length; r++)
                AssertRangeMatches(values, tree, l, r);
    }
}