using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Controls;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Theming;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The single stateful root control for the Rodel Timer product UI. It owns every piece of UI-only reactive state
/// (viewport size, compact-presenter flag, the in-progress session title, and transient status),
/// hosts the central timer content, drives the
/// one-second UI tick into the store, and orchestrates every dialog and menu. The reactive
/// <see cref="TimerAppStore"/> is the data source it binds to; the store is owned and disposed by <see cref="App"/>.
/// </summary>
public sealed partial class MainView : Control, ILanguageApplyTarget
{
    private readonly Window _window;
    private readonly TimerAppStore _store;
    private readonly IReadOnlyList<FontChoice> _fonts;
    private readonly TimerChrome _chrome;
    private readonly TimerWindowBackground _windowBackground;

    /// <summary>Creates the product view over a window and its reactive store.</summary>
    /// <param name="window">The hosting window (the source of theme, presenter, and dialog hosting).</param>
    /// <param name="store">The reactive timer store this view renders and mutates.</param>
    /// <param name="fonts">The selectable UI fonts offered in settings.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public MainView(Window window, TimerAppStore store, IReadOnlyList<FontChoice> fonts)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        _chrome = new TimerChrome(window, this);
        _windowBackground = new TimerWindowBackground(window);
        ViewportWidth = 1100d;
        ViewportHeight = 720d;
        SessionTitle = string.Empty;
        SaveErrorMessage = string.Empty;
    }

    /// <summary>The current viewport width in effective pixels.</summary>
    public partial double ViewportWidth { get; private set; }

    /// <summary>The current viewport height in effective pixels.</summary>
    public partial double ViewportHeight { get; private set; }

    /// <summary>Whether the window is currently in the compact overlay presenter.</summary>
    public partial bool IsCompactPresenter { get; private set; }

    /// <summary>The in-progress session title being edited; UI-only until a session is stopped.</summary>
    public partial string SessionTitle { get; private set; }

    /// <summary>A monotonic counter bumped on every theme change so descendant controls re-resolve their colors.</summary>
    public partial int ThemeStamp { get; private set; }

    /// <summary>A blocking save-failure message shown over the shell, or the empty string when there is none.</summary>
    /// <remarks>
    /// A non-null <see cref="string"/> (empty = "no message") rather than <c>string?</c> because the reactive store
    /// generator emits its partial-property implementation with the fully qualified type, which drops the nullable
    /// reference annotation — a <c>string?</c> author declaration would mismatch the generated <c>string</c> (CS9256).
    /// </remarks>
    public partial string SaveErrorMessage { get; private set; }

    /// <summary>The store this view binds to (shared with the slot controls and rows).</summary>
    internal TimerAppStore Store => _store;

    /// <summary>The hosting window (shared with the slot controls for theme and dialog hosting).</summary>
    internal Window Window => _window;

    /// <summary>The selectable UI fonts offered in settings.</summary>
    internal IReadOnlyList<FontChoice> Fonts => _fonts;

    /// <summary>The current folders, or an empty list before the workspace has loaded (never throws).</summary>
    internal IReadOnlyList<Core.Folders.TimerFolder> Folders =>
        _store.Bootstrap.IsSuccess ? _store.Folders : [];

    /// <summary>The selected folder's history entries, or an empty list before the workspace has loaded (never throws).</summary>
    internal IReadOnlyList<Core.History.TimerEntry> SelectedEntries =>
        _store.Bootstrap.IsSuccess ? _store.SelectedEntries : [];

    /// <summary>The selected folder's id, or <see cref="Guid.Empty"/> before the workspace has loaded (never throws).</summary>
    internal Guid SelectedFolderId =>
        _store.Bootstrap.IsSuccess ? _store.SelectedFolder.Id : Guid.Empty;

    /// <summary>The selected folder's name, or the empty string before the workspace has loaded (never throws).</summary>
    internal string SelectedFolderName =>
        _store.Bootstrap.IsSuccess ? _store.SelectedFolder.Name : string.Empty;

    /// <summary>The current preferences, or the defaults before the workspace has loaded (never throws).</summary>
    internal TimerPreferences Preferences =>
        _store.Bootstrap.IsSuccess ? _store.Preferences : TimerPreferences.Default;

    /// <summary>The today's completed total, or zero before the workspace has loaded (never throws).</summary>
    internal TimeSpan TodayTotal =>
        _store.Bootstrap.IsSuccess ? _store.TodayTotal : TimeSpan.Zero;

    /// <summary>Whether a timing session is currently running.</summary>
    internal bool IsRunning => _store.IsRunning;

    /// <summary>The running session's elapsed time (zero when idle); safe to read before the workspace loads.</summary>
    internal TimeSpan Elapsed => _store.Elapsed;

    /// <summary>The plain command-and-theme surface handed to the render-only composites.</summary>
    internal TimerChrome Chrome => _chrome;

    /// <summary>The footer "today's total / target" summary line (localized, culture-formatted).</summary>
    internal string SummaryText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        "{0}: {1} / {2} {3}",
        Resources.TodayTotal,
        TimerFormat.WholeMinutes(TodayTotal),
        Preferences.DailyTargetMinutes,
        Resources.MinutesUnit);

    /// <summary>The footer's compact completed/target value, without repeating its localized label.</summary>
    internal string FocusSummaryText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        "{0} / {1} {2}",
        TimerFormat.WholeMinutes(TodayTotal),
        Preferences.DailyTargetMinutes,
        Resources.MinutesUnit);

    /// <summary>The localized requested material and its post-apply active/fallback state.</summary>
    internal string BackgroundStatusText
    {
        get
        {
            _ = MaterialPolicy.Current.System;
            TimerBackgroundResolution resolution = _windowBackground.Resolve(Preferences.Background);
            string background = resolution.Requested switch
            {
                TimerBackgroundMode.Mica => Resources.BackgroundStatusMica,
                TimerBackgroundMode.MicaAlt => Resources.BackgroundStatusMicaAlt,
                TimerBackgroundMode.Acrylic => Resources.BackgroundStatusAcrylic,
                TimerBackgroundMode.AcrylicThin => Resources.BackgroundStatusAcrylicThin,
                _ => throw new InvalidOperationException("The persisted background mode is not supported."),
            };
            string state = resolution.IsFallback
                ? Resources.BackgroundStatusFallback
                : Resources.BackgroundStatusActive;
            return $"{background} \u00B7 {state}";
        }
    }

    /// <summary>
    /// Whether the requested window material is currently showing its opaque policy fallback. The footer surfaces the
    /// material line only in this degraded state — the active case is the silent default, so the middle column stays a
    /// quiet running-state indicator rather than a persistent "material active" readout.
    /// </summary>
    internal bool IsBackgroundFallback
    {
        get
        {
            _ = MaterialPolicy.Current.System;
            return _windowBackground.Resolve(Preferences.Background).IsFallback;
        }
    }

    /// <summary>The blocking-message title (save-failed when a save error is pending, else the load-error title).</summary>
    internal string MessageTitle =>
        SaveErrorMessage.Length > 0 ? Resources.SaveFailed : Resources.DataLoadErrorTitle;

    /// <summary>The blocking-message detail (the save-error text, else the bootstrap failure message).</summary>
    internal string MessageDetail =>
        SaveErrorMessage.Length > 0 ? SaveErrorMessage : _store.Bootstrap.Error?.Message ?? string.Empty;

    /// <inheritdoc/>
    /// <remarks>
    /// The normal workspace is the only resident content tree; Folders and History live in title-bar-anchored Flyouts,
    /// so their closed state contributes no hidden focus or accessibility nodes. The loading and
    /// blocking-message overlays use <c>When(...)</c> rather than
    /// <c>.Visible(...)</c> so a hidden overlay is torn out of all three trees (a collapsed <c>.Visible(false)</c>
    /// subtree keeps its semantics node, which would leak the retry button into the accessibility tree while the shell
    /// is healthy).
    /// </remarks>
    public Ui Body => Overlay(
        new TimerContentControl(Chrome),
        When(IsBootstrapping, new TimerLoadingView(Chrome, ThemeStamp)),
        When(HasBlockingMessage, new TimerMessageView(Chrome, MessageTitle, MessageDetail, ThemeStamp)));

    /// <inheritdoc/>
    protected override IDisposable? OnMounted()
    {
        DispatcherQueueTimer timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.IsRepeating = true;
        timer.Tick += OnTick;
        timer.Start();
        return new TickSubscription(timer, OnTick);
    }

    private void OnTick(object? sender, EventArgs e) => _store.Tick();

    private bool IsBootstrapping => !_store.Bootstrap.IsSuccess && !_store.Bootstrap.IsError;

    private bool HasBlockingMessage => _store.Bootstrap.IsError || SaveErrorMessage.Length > 0;

    private sealed class TickSubscription(DispatcherQueueTimer timer, EventHandler handler) : IDisposable
    {
        private DispatcherQueueTimer? _timer = timer;

        public void Dispose()
        {
            if (_timer is null)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= handler;
            _timer = null;
        }
    }
}
