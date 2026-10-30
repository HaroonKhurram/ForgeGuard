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

        // Try every range l..r and compare with a plain loop
        for (int l = 0; l < values.Length; l++)
        {
            for (int r = l; r < values.Length; r++)
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
        }
    }
}