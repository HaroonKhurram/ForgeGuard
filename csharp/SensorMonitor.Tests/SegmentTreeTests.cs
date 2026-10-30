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
}