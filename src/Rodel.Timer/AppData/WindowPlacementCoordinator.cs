using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Graphics;

namespace Rodel.Timer.AppData;

/// <summary>Coordinates mode-specific placement persistence with the live window.</summary>
internal static class WindowPlacementCoordinator
{
    /// <summary>
    /// Loads only the overlapped placement for startup. If the new slot is empty, a valid legacy shared value is
    /// classified and copied to the safest mode-specific slot before startup continues.
    /// </summary>
    public static WindowPlacement? LoadOverlappedForStartup(Size compactMaximum)
    {
        if (TryLoad(TimerWindowPlacementMode.Overlapped, out WindowPlacement placement))
        {
            return placement;
        }

        string? legacy = WindowPlacementStore.LoadLegacy();
        if (!WindowPlacement.TryParse(legacy, out placement))
        {
            return null;
        }

        TimerWindowPlacementMode mode =
            TimerWindowPlacementPolicy.ClassifyLegacy(placement, compactMaximum);
        if (WindowPlacementStore.Save(mode, legacy!))
        {
            WindowPlacementStore.RemoveLegacy();
        }

        return mode == TimerWindowPlacementMode.Overlapped ? placement : null;
    }

    /// <summary>Saves the live bounds into the slot owned by the current presenter.</summary>
    public static void SaveCurrent(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        TimerWindowPlacementMode mode =
            TimerWindowPlacementPolicy.ForPresenter(window.Presenter.Kind);
        WindowPlacementStore.Save(mode, window.SavePlacement().Serialize());
    }

    /// <summary>Restores a saved mode-specific placement into the current presenter, when one exists.</summary>
    public static void Restore(Window window, TimerWindowPlacementMode mode)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (TryLoad(mode, out WindowPlacement placement))
        {
            window.RestorePlacement(placement);
        }
    }

    private static bool TryLoad(
        TimerWindowPlacementMode mode,
        out WindowPlacement placement) =>
        WindowPlacement.TryParse(WindowPlacementStore.Load(mode), out placement);
}
