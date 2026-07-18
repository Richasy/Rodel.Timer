using Sprout;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The resident timer content. It resolves every reactive input (viewport, elapsed clock, running / compact flags,
/// title, footer state, folder name, and theme) from
/// its stable <see cref="TimerChrome"/> surface, then branches on the compact-overlay flag: the full
/// <see cref="NormalTimerView"/> (hero card + footer command dock) or the reduced <see cref="CompactTimerView"/> (only
/// the clock, the start/stop circle, and an exit chip). The branch is a <c>When(IsCompact, …)</c> switch, so toggling
/// the compact overlay <b>tears the entire inactive branch out of the interaction and accessibility trees</b> — the
/// title and footer nodes are gone while compact, and return on exit. The brand / settings / compact command lives in
/// the extended title bar (<see cref="TimerTitleBarControl"/>), not this content.
/// </summary>
/// <remarks>Reading every live value here subscribes this control and threads updates into the retained branch.</remarks>
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
                Rodel.Timer.Presentation.TimerCompactLayoutResolver.Resolve(
                    _chrome.ViewportWidth,
                    _chrome.ViewportHeight),
                _chrome.SelectedFolderName,
                _chrome.Elapsed,
                _chrome.IsRunning,
                _chrome.IsRunning ? Resources.TimerRunning : Resources.TimerReady),
            new NormalTimerView(
                _chrome,
                Colors,
                Rodel.Timer.Presentation.TimerNormalLayoutResolver.Resolve(
                    _chrome.ViewportWidth,
                    _chrome.ViewportHeight),
                _chrome.SelectedFolderName,
                _chrome.Elapsed,
                _chrome.IsRunning,
                _chrome.SessionTitle,
                _chrome.FocusSummaryText,
                _chrome.IsBackgroundFallback,
                _chrome.BackgroundStatusText,
                Resources.CurrentFolder,
                Resources.TodaysFocus,
                _chrome.IsRunning ? Resources.TimerRunning : Resources.TimerReady,
                _chrome.IsRunning ? Resources.StopTimer : Resources.StartTimer)));

    private Sprout.Theming.ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }
}
