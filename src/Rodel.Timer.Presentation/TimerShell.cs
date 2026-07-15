using Sprout.Controls;

namespace Rodel.Timer.Presentation;

/// <summary>The fully resolved shell layout for the nested folder/history split views at a given moment.</summary>
/// <param name="FolderMode">The outer (folder) pane display mode.</param>
/// <param name="HistoryMode">The inner (history) pane display mode.</param>
/// <param name="FolderPaneOpen">Whether the outer (folder) pane is effectively open.</param>
/// <param name="HistoryPaneOpen">Whether the inner (history) pane is effectively open.</param>
public readonly record struct TimerShellLayout(
    SplitViewDisplayMode FolderMode,
    SplitViewDisplayMode HistoryMode,
    bool FolderPaneOpen,
    bool HistoryPaneOpen);

/// <summary>
/// Pure, testable resolution of the shell's split-view layout from the viewport width, the compact-overlay presenter
/// flag, and the two UI-only pane-open flags. In the compact overlay presenter both panes are forced overlay and
/// closed (the mini timer shows only the core timer); otherwise the modes come from
/// <see cref="TimerBreakpoints.Resolve(double)"/> and the panes honour their open flags.
/// </summary>
public static class TimerShell
{
    /// <summary>Resolves the effective shell layout.</summary>
    /// <param name="viewportWidth">The current viewport width in effective pixels.</param>
    /// <param name="isCompact">Whether the window is in the compact overlay presenter.</param>
    /// <param name="folderPaneOpen">Whether the folder pane's open flag is set.</param>
    /// <param name="historyPaneOpen">Whether the history pane's open flag is set.</param>
    /// <returns>The resolved display modes and effective open states for both panes.</returns>
    public static TimerShellLayout Resolve(
        double viewportWidth,
        bool isCompact,
        bool folderPaneOpen,
        bool historyPaneOpen)
    {
        if (isCompact)
        {
            return new TimerShellLayout(
                SplitViewDisplayMode.Overlay,
                SplitViewDisplayMode.Overlay,
                FolderPaneOpen: false,
                HistoryPaneOpen: false);
        }

        TimerPaneModes modes = TimerBreakpoints.Resolve(viewportWidth);
        return new TimerShellLayout(modes.Folder, modes.History, folderPaneOpen, historyPaneOpen);
    }

    /// <summary>
    /// Whether the two panes are <b>mutually exclusive</b> at this layout — i.e. opening one must close the other. That
    /// is true exactly when <b>both</b> panes are overlay drawers (the narrow layout at or below
    /// <see cref="TimerBreakpoints.MediumThreshold"/>): two overlay drawers over the same content would stack and hide
    /// it, so only one may be open. In the medium layout (folder overlay + history inline) and the wide layout (both
    /// inline) the panes coexist, so they are not mutually exclusive. In the compact overlay presenter both panes are
    /// forced closed, so exclusivity is moot and reported <see langword="false"/>.
    /// </summary>
    /// <param name="viewportWidth">The current viewport width in effective pixels.</param>
    /// <param name="isCompact">Whether the window is in the compact overlay presenter.</param>
    /// <returns><see langword="true"/> when opening one pane must close the other; otherwise <see langword="false"/>.</returns>
    public static bool PanesAreMutuallyExclusive(double viewportWidth, bool isCompact)
    {
        if (isCompact)
        {
            return false;
        }

        TimerPaneModes modes = TimerBreakpoints.Resolve(viewportWidth);
        return modes.Folder == SplitViewDisplayMode.Overlay
            && modes.History == SplitViewDisplayMode.Overlay;
    }
}
