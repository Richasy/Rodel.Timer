namespace Rodel.Timer.Presentation;

/// <summary>The window capabilities and title-bar command visibility for the current pin/maximize state.</summary>
/// <param name="IsAlwaysOnTop">Whether the overlapped window stays above other windows.</param>
/// <param name="IsResizable">Whether the user can resize the window.</param>
/// <param name="IsMaximizable">Whether the window exposes maximize behavior.</param>
/// <param name="ShowCompactOverlayButton">Whether the compact-overlay command is present.</param>
/// <param name="ShowPinButton">Whether the pin toggle is present.</param>
public readonly record struct TimerWindowPinBehavior(
    bool IsAlwaysOnTop,
    bool IsResizable,
    bool IsMaximizable,
    bool ShowCompactOverlayButton,
    bool ShowPinButton);

/// <summary>Resolves the pinned-window behavior shared by the window host and title bar.</summary>
public static class TimerWindowPinPolicy
{
    /// <summary>Resolves the capabilities and command visibility for the current state.</summary>
    /// <param name="isPinned">Whether the full timer window is pinned above other windows.</param>
    /// <param name="isMaximized">Whether the overlapped window is currently maximized.</param>
    /// <returns>The complete pinned-window behavior.</returns>
    public static TimerWindowPinBehavior Resolve(bool isPinned, bool isMaximized) => new(
        IsAlwaysOnTop: isPinned,
        IsResizable: true,
        IsMaximizable: !isPinned,
        ShowCompactOverlayButton: !isPinned,
        ShowPinButton: !isMaximized);
}
