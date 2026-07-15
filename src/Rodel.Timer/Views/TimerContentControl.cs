using Sprout;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The central timer content hosted in the inner split view's content slot. It resolves every reactive input (the
/// elapsed clock, the running / compact flags, the title, the summary, the folder name, the font, and the theme) from
/// its stable <see cref="TimerChrome"/> surface, then branches on the compact-overlay flag: the full
/// <see cref="NormalTimerView"/> (hero card + footer command dock) or the reduced <see cref="CompactTimerView"/> (only
/// the clock, the start/stop circle, and an exit chip). The branch is a <c>When(IsCompact, …)</c> switch, so toggling
/// the compact overlay <b>tears the entire inactive branch out of the interaction and accessibility trees</b> — the
/// title and footer nodes are gone while compact, and return on exit. The brand / settings / compact command lives in
/// the extended title bar (<see cref="TimerTitleBarControl"/>), not this content.
/// </summary>
/// <remarks>
/// It is a <see cref="Control"/> (not a render-only value view) so the split view's <c>(Control, Control)</c> overload
/// wires its interaction and semantics trees — every timer button is clickable, focusable, and screen-reader reachable.
/// Because the normal branch carries several interactive children, each control gets its own hit bounds rather than
/// inheriting a container's. Reading each reactive value here (rather than inside the branch views) subscribes this
/// control's reactive effect, so a tick, a theme switch, or a title edit re-renders it in place and threads the fresh
/// field values into the retained branch view.
/// </remarks>
public sealed partial class TimerContentControl : Control
{
    private readonly TimerChrome _chrome;

    /// <summary>Creates the timer content over its command-and-read surface.</summary>
    /// <param name="chrome">The plain command-and-read surface (callbacks + live state + colours).</param>
    /// <exception cref="ArgumentNullException"><paramref name="chrome"/> is <see langword="null"/>.</exception>
    public TimerContentControl(TimerChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
    }

    /// <summary>The compact or full timer content, chosen by the live compact-overlay flag.</summary>
    public Ui Body => Overlay(
        When(
            _chrome.IsCompact,
            new CompactTimerView(
                _chrome,
                Colors,
                _chrome.FontFamily,
                _chrome.Elapsed,
                _chrome.IsRunning),
            new NormalTimerView(
                _chrome,
                Colors,
                _chrome.FontFamily,
                _chrome.SelectedFolderName,
                _chrome.Elapsed,
                _chrome.IsRunning,
                _chrome.SessionTitle,
                _chrome.SummaryText)));

    private Sprout.Theming.ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }
}
