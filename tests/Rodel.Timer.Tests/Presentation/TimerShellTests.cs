using Rodel.Timer.Presentation;

using Sprout.Controls;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Pure-function coverage of <see cref="TimerShell"/> — the compact-overlay override on top of the responsive
/// breakpoints. Compact forces both panes overlay and closed (mini timer shows only the core); otherwise the panes
/// honour their open flags and the breakpoint modes.
/// </summary>
[TestClass]
public sealed class TimerShellTests
{
    [TestMethod]
    public void Compact_ForcesBothPanesOverlayAndClosed()
    {
        TimerShellLayout layout = TimerShell.Resolve(1400d, isCompact: true, folderPaneOpen: true, historyPaneOpen: true);

        Assert.AreEqual(SplitViewDisplayMode.Overlay, layout.FolderMode);
        Assert.AreEqual(SplitViewDisplayMode.Overlay, layout.HistoryMode);
        Assert.IsFalse(layout.FolderPaneOpen, "compact closes the folder pane regardless of its open flag.");
        Assert.IsFalse(layout.HistoryPaneOpen, "compact closes the history pane regardless of its open flag.");
    }

    [TestMethod]
    public void Wide_NotCompact_HonoursOpenFlagsAndInlineModes()
    {
        TimerShellLayout layout = TimerShell.Resolve(1400d, isCompact: false, folderPaneOpen: true, historyPaneOpen: false);

        Assert.AreEqual(SplitViewDisplayMode.Inline, layout.FolderMode);
        Assert.AreEqual(SplitViewDisplayMode.Inline, layout.HistoryMode);
        Assert.IsTrue(layout.FolderPaneOpen);
        Assert.IsFalse(layout.HistoryPaneOpen);
    }

    [TestMethod]
    public void Narrow_NotCompact_OverlayModesWithOpenFlags()
    {
        TimerShellLayout layout = TimerShell.Resolve(700d, isCompact: false, folderPaneOpen: false, historyPaneOpen: true);

        Assert.AreEqual(SplitViewDisplayMode.Overlay, layout.FolderMode);
        Assert.AreEqual(SplitViewDisplayMode.Overlay, layout.HistoryMode);
        Assert.IsFalse(layout.FolderPaneOpen);
        Assert.IsTrue(layout.HistoryPaneOpen);
    }

    [TestMethod]
    public void Medium_NotCompact_FolderOverlayHistoryInline()
    {
        TimerShellLayout layout = TimerShell.Resolve(1000d, isCompact: false, folderPaneOpen: true, historyPaneOpen: true);

        Assert.AreEqual(SplitViewDisplayMode.Overlay, layout.FolderMode);
        Assert.AreEqual(SplitViewDisplayMode.Inline, layout.HistoryMode);
    }

    [TestMethod]
    public void PanesAreMutuallyExclusive_NarrowBothOverlay_True()
    {
        // Narrow: both panes overlay, so opening one must close the other.
        Assert.IsTrue(TimerShell.PanesAreMutuallyExclusive(700d, isCompact: false));
    }

    [TestMethod]
    public void PanesAreMutuallyExclusive_MediumMixedModes_False()
    {
        // Medium: folder overlay + history inline can coexist, so not exclusive.
        Assert.IsFalse(TimerShell.PanesAreMutuallyExclusive(1000d, isCompact: false));
    }

    [TestMethod]
    public void PanesAreMutuallyExclusive_WideBothInline_False()
    {
        // Wide: both inline drawers coexist.
        Assert.IsFalse(TimerShell.PanesAreMutuallyExclusive(1400d, isCompact: false));
    }

    [TestMethod]
    public void PanesAreMutuallyExclusive_Compact_False()
    {
        // Compact forces both panes closed, so exclusivity is moot.
        Assert.IsFalse(TimerShell.PanesAreMutuallyExclusive(700d, isCompact: true));
    }
}
