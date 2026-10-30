using SensorMonitor.Core.SegmentTrees;

namespace SensorMonitor.Tests;

public class SegmentTreeValidationTests
{
    private static SegmentTree BuiltTree()
    {
        var tree = new SegmentTree();
        tree.Build(new double[] { 5, 2, 8, 1 });
        return tree;
    }

    [Fact]
    public void Build_Null_Throws()
    {
        var tree = new SegmentTree();

        Assert.Throws<ArgumentNullException>(() => tree.Build(null!));
    }

    [Fact]
    public void Build_EmptyArray_Throws()
    {
        var tree = new SegmentTree();

        Assert.Throws<ArgumentException>(() => tree.Build(Array.Empty<double>()));
    }

    [Fact]
    public void Queries_BeforeBuild_Throw()
    {
        var tree = new SegmentTree();

        Assert.Throws<InvalidOperationException>(() => { _ = tree.QuerySum(0, 0); });
        Assert.Throws<InvalidOperationException>(() => { _ = tree.QueryMax(0, 0); });
        Assert.Throws<InvalidOperationException>(() => { _ = tree.QueryMin(0, 0); });
    }

    [Fact]
    public void Update_BeforeBuild_Throws()
    {
        var tree = new SegmentTree();

        Assert.Throws<InvalidOperationException>(() => tree.Update(0, 1));
    }

    [Fact]
    public void RootValues_BeforeBuild_Throw()
    {
        var tree = new SegmentTree();

        Assert.Throws<InvalidOperationException>(() => { _ = tree.RootMax; });
        Assert.Throws<InvalidOperationException>(() => { _ = tree.RootMin; });
        Assert.Throws<InvalidOperationException>(() => { _ = tree.RootSum; });
    }

    [Theory]
    [InlineData(2, 1)]    // start after end
    [InlineData(-1, 2)]   // start below 0
    [InlineData(0, 4)]    // end one past the last position
    [InlineData(0, 100)]  // end far past the last position
    [InlineData(4, 4)]    // single position that does not exist
    public void Queries_BadRange_Throw(int l, int r)
    {
        var tree = BuiltTree();

        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = tree.QuerySum(l, r); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = tree.QueryMax(l, r); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = tree.QueryMin(l, r); });
    }

    [Fact]
    public void Queries_EdgeRanges_StillWork()
    {
        var tree = BuiltTree();

        Assert.Equal(16, tree.QuerySum(0, 3)); // the whole array
        Assert.Equal(5, tree.QueryMax(0, 0));  // first position only
        Assert.Equal(1, tree.QueryMin(3, 3));  // last position only
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public void Update_BadIndex_Throws(int index)
    {
        var tree = BuiltTree();

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Update(index, 99));
    }

    [Fact]
    public void Update_BadIndex_LeavesTreeUnchanged()
    {
        var tree = BuiltTree();

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Update(10, 99));

        Assert.Equal(16, tree.RootSum);
        Assert.Equal(8, tree.RootMax);
        Assert.Equal(1, tree.RootMin);
    }
}