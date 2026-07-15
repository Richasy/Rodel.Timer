using Sprout.Controls;

namespace Rodel.Timer.Presentation;

/// <summary>The resolved pane display modes for the nested folder/history split views at a given viewport width.</summary>
/// <param name="Folder">The outer (folder) pane display mode.</param>
/// <param name="History">The inner (history) pane display mode.</param>
public readonly record struct TimerPaneModes(
    SplitViewDisplayMode Folder,
    SplitViewDisplayMode History);

/// <summary>
/// Pure, testable viewport breakpoint logic for the shell. The two nested split views collapse from inline
/// drawers to overlay drawers as the window narrows, matching the legacy layout:
/// <list type="bullet">
/// <item><description>wider than <see cref="WideThreshold"/>: both panes inline;</description></item>
/// <item><description>between <see cref="MediumThreshold"/> and <see cref="WideThreshold"/>: folder overlay, history inline;</description></item>
/// <item><description>at or below <see cref="MediumThreshold"/>: both panes overlay.</description></item>
/// </list>
/// Only <see cref="SplitViewDisplayMode.Overlay"/> and <see cref="SplitViewDisplayMode.Inline"/> are used (no
/// zero-width compact rail), so a closed pane takes no layout space.
/// </summary>
public static class TimerBreakpoints
{
    /// <summary>The viewport width (effective pixels) above which both panes may be inline.</summary>
    public const double WideThreshold = 1280d;

    /// <summary>The viewport width (effective pixels) above which the history pane may be inline.</summary>
    public const double MediumThreshold = 820d;

    /// <summary>Resolves the folder/history pane display modes for a viewport width in effective pixels.</summary>
    /// <param name="viewportWidth">The current viewport width in effective pixels.</param>
    /// <returns>The pane modes to apply to the outer and inner split views.</returns>
    public static TimerPaneModes Resolve(double viewportWidth)
    {
        if (viewportWidth > WideThreshold)
        {
            return new TimerPaneModes(SplitViewDisplayMode.Inline, SplitViewDisplayMode.Inline);
        }

        if (viewportWidth > MediumThreshold)
        {
            return new TimerPaneModes(SplitViewDisplayMode.Overlay, SplitViewDisplayMode.Inline);
        }

        return new TimerPaneModes(SplitViewDisplayMode.Overlay, SplitViewDisplayMode.Overlay);
    }
}
