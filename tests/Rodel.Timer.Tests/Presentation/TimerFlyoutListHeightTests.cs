using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// The folder/history flyout list is given a DEFINITE height (not a bare max cap) so the ItemsView scroller
/// activates: a definite height makes the composite measure tight, and the OverlayPanel measures its child loosely
/// against that maximum, so the inner ScrollView sees a bounded viewport &lt; content and scrolls. This pins the
/// hug-then-cap contract the height resolver encodes: a tiny list holds the empty-state minimum, a mid list hugs its
/// content exactly (no empty tail — the round-4 complaint), and a long list caps at the maximum (beyond which it
/// scrolls internally — the round-7 complaint), all without re-hard-coding the row-stride arithmetic here.
/// </summary>
[TestClass]
public sealed class TimerFlyoutListHeightTests
{
    private const float Delta = 0.01f;

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void TinyList_ClampsToTheEmptyStateMinimum(int rowCount)
    {
        // 0 rows (12) and 1 row (66) are both below the 80 minimum that holds the centered empty-state text.
        Assert.AreEqual(
            TimerAnchoredSurfaceLayoutResolver.MinimumListHeight,
            TimerAnchoredSurfaceLayoutResolver.ListHeight(rowCount),
            Delta);
    }

    [TestMethod]
    public void MidList_HugsItsContentExactly()
    {
        // 4 rows = 4*54 + 12 = 228, strictly between the min and the cap → the exact content height (a hug, no tail).
        float height = TimerAnchoredSurfaceLayoutResolver.ListHeight(4);

        Assert.AreEqual(228f, height, Delta);
        Assert.IsGreaterThan(TimerAnchoredSurfaceLayoutResolver.MinimumListHeight, height);
        Assert.IsLessThan(TimerAnchoredSurfaceLayoutResolver.MaximumListHeight, height);
    }

    [TestMethod]
    [DataRow(6)]
    [DataRow(12)]
    [DataRow(35)]
    public void LongList_CapsAtTheMaximumSoItScrolls(int rowCount)
    {
        // From 6 rows (336) upward the content exceeds the cap, so the list caps and the ItemsView scrolls internally.
        Assert.AreEqual(
            TimerAnchoredSurfaceLayoutResolver.MaximumListHeight,
            TimerAnchoredSurfaceLayoutResolver.ListHeight(rowCount),
            Delta);
    }

    [TestMethod]
    public void ListHeight_IsMonotonicUntilTheCap()
    {
        // A hug means each extra row (until the cap) makes the surface taller — never shorter.
        float previous = TimerAnchoredSurfaceLayoutResolver.ListHeight(0);
        for (int rowCount = 1; rowCount <= 40; rowCount++)
        {
            float height = TimerAnchoredSurfaceLayoutResolver.ListHeight(rowCount);
            Assert.IsGreaterThanOrEqualTo(previous, height, $"height regressed at {rowCount} rows: {height} < {previous}");
            previous = height;
        }
    }

    [TestMethod]
    public void MaximumList_LeavesRoomForTheHeaderUnderTheSurfaceCap()
    {
        // The list cap must leave room for the flyout header + divider under the surface maximum, so the surface never
        // itself clips the list — the list scrolls internally instead of the whole flyout being cut off.
        Assert.IsLessThan(
            TimerAnchoredSurfaceLayoutResolver.MaximumHeight,
            TimerAnchoredSurfaceLayoutResolver.MaximumListHeight,
            "the list cap must sit below the surface cap so the header fits above the list");
    }
}
