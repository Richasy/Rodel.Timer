using Rodel.Timer.Presentation;

using Sprout.Controls;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Pure-function coverage of <see cref="TimerBreakpoints"/> — the viewport width thresholds that collapse the nested
/// folder/history split views from inline drawers to overlay drawers as the window narrows.
/// </summary>
[TestClass]
public sealed class TimerBreakpointsTests
{
    [TestMethod]
    public void Wide_BothPanesInline()
    {
        TimerPaneModes modes = TimerBreakpoints.Resolve(1400d);

        Assert.AreEqual(SplitViewDisplayMode.Inline, modes.Folder);
        Assert.AreEqual(SplitViewDisplayMode.Inline, modes.History);
    }

    [TestMethod]
    public void Medium_FolderOverlayHistoryInline()
    {
        TimerPaneModes modes = TimerBreakpoints.Resolve(1000d);

        Assert.AreEqual(SplitViewDisplayMode.Overlay, modes.Folder);
        Assert.AreEqual(SplitViewDisplayMode.Inline, modes.History);
    }

    [TestMethod]
    public void Narrow_BothPanesOverlay()
    {
        TimerPaneModes modes = TimerBreakpoints.Resolve(700d);

        Assert.AreEqual(SplitViewDisplayMode.Overlay, modes.Folder);
        Assert.AreEqual(SplitViewDisplayMode.Overlay, modes.History);
    }

    [TestMethod]
    public void WideThreshold_IsExclusiveLowerBoundForBothInline()
    {
        // Exactly at the wide threshold is still "medium" (folder overlay); just above flips folder to inline.
        Assert.AreEqual(SplitViewDisplayMode.Overlay, TimerBreakpoints.Resolve(TimerBreakpoints.WideThreshold).Folder);
        Assert.AreEqual(SplitViewDisplayMode.Inline, TimerBreakpoints.Resolve(TimerBreakpoints.WideThreshold + 0.5d).Folder);
    }

    [TestMethod]
    public void MediumThreshold_IsExclusiveLowerBoundForHistoryInline()
    {
        // Exactly at the medium threshold both panes overlay; just above flips history to inline.
        Assert.AreEqual(SplitViewDisplayMode.Overlay, TimerBreakpoints.Resolve(TimerBreakpoints.MediumThreshold).History);
        Assert.AreEqual(SplitViewDisplayMode.Inline, TimerBreakpoints.Resolve(TimerBreakpoints.MediumThreshold + 0.5d).History);
    }

    [TestMethod]
    public void OnlyOverlayAndInlineModesAreUsed()
    {
        foreach (double width in new[] { 0d, 500d, 900d, 901d, 1300d, 1301d, 4000d })
        {
            TimerPaneModes modes = TimerBreakpoints.Resolve(width);
            Assert.IsTrue(
                modes.Folder is SplitViewDisplayMode.Overlay or SplitViewDisplayMode.Inline,
                $"folder mode at {width} must be overlay or inline (no compact rail).");
            Assert.IsTrue(
                modes.History is SplitViewDisplayMode.Overlay or SplitViewDisplayMode.Inline,
                $"history mode at {width} must be overlay or inline (no compact rail).");
        }
    }
}
