using Sprout;
using Sprout.Graphics;

namespace Rodel.Timer.Presentation;

/// <summary>The independently persisted placement modes used by the timer window.</summary>
public enum TimerWindowPlacementMode
{
    /// <summary>The standard overlapped window.</summary>
    Overlapped,

    /// <summary>The always-on-top compact-overlay window.</summary>
    CompactOverlay,
}

/// <summary>Resolves placement modes and safely classifies the legacy shared placement value.</summary>
public static class TimerWindowPlacementPolicy
{
    /// <summary>Maps a presenter to the placement slot that owns its bounds.</summary>
    /// <param name="presenter">The current presenter kind.</param>
    /// <returns>The placement mode for persistence.</returns>
    public static TimerWindowPlacementMode ForPresenter(WindowPresenterKind presenter) =>
        presenter == WindowPresenterKind.CompactOverlay
            ? TimerWindowPlacementMode.CompactOverlay
            : TimerWindowPlacementMode.Overlapped;

    /// <summary>
    /// Classifies a value written before placements were split. A maximized value can only be overlapped. For normal
    /// values, anything within the compact overlay's full legal range is assigned to compact; only a rectangle exceeding
    /// that range is safely known to be overlapped. This intentionally favors compact in the ambiguous overlap so legacy
    /// compact bounds can never poison the standard startup window again.
    /// </summary>
    /// <param name="placement">The parsed legacy placement.</param>
    /// <param name="compactMaximum">The compact-overlay window's maximum client size in effective pixels.</param>
    /// <returns>The safest destination slot for the legacy value.</returns>
    public static TimerWindowPlacementMode ClassifyLegacy(
        WindowPlacement placement,
        Size compactMaximum)
    {
        if (placement.IsMaximized)
        {
            return TimerWindowPlacementMode.Overlapped;
        }

        double dpiScale = placement.Dpi / 96d;
        double width = placement.Width / dpiScale;
        double height = placement.Height / dpiScale;
        return width > compactMaximum.Width || height > compactMaximum.Height
            ? TimerWindowPlacementMode.Overlapped
            : TimerWindowPlacementMode.CompactOverlay;
    }
}
