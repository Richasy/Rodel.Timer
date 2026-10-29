using Rodel.Timer.AppData;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Localization;
using Sprout.Semantics;
using Sprout.Theming;

namespace Rodel.Timer;

/// <summary>Command and state-mutation surface of <see cref="MainView"/> (invoked by the shell, slots, rows, and dialogs).</summary>
public sealed partial class MainView
{
    /// <summary>Updates the effective-pixel viewport used by the normal and compact layout resolvers.</summary>
    /// <param name="width">The client width.</param>
    /// <param name="height">The client height.</param>
    public void SetViewportSize(double width, double height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
    }

    /// <summary>Records the current compact-overlay presenter state.</summary>
    /// <param name="isCompact">Whether the window is now in the compact overlay presenter.</param>
    public void SetCompactPresenter(bool isCompact)
    {
        IsCompactPresenter = isCompact;

        // The compact overlay has no footer commands and no room for a panel, so an open side panel is dropped on the
        // way in rather than left floating over the mini timer.
        if (isCompact)
        {
            CloseSidePanelImmediately();
        }

        // Collapse the extended title bar to the standard 32 epx band while compact (its brand + command band is also
        // torn out of all three trees by the title bar's When(!IsCompact) switch), and restore the 48 epx tall band on
        // exit so the brand + settings + compact commands return. The window material is degraded to the opaque themed
        // fallback automatically by the framework while the window is a topmost compact overlay, and restored on exit.
        _window.TitleBarHeight = isCompact ? TitleBarHeight.Standard : TitleBarHeight.Tall;
    }

    /// <summary>Records whether the overlapped window is currently maximized.</summary>
    /// <param name="isMaximized">Whether the window is maximized.</param>
    public void SetWindowMaximized(bool isMaximized) => IsWindowMaximized = isMaximized;

    /// <summary>Applies or removes the full-window always-on-top mode.</summary>
    /// <param name="isPinned">The controlled two-state value emitted by the title-bar toggle.</param>
    public void SetWindowPinned(bool? isPinned)
    {
        bool pin = isPinned ?? throw new ArgumentNullException(nameof(isPinned));
        if (_window.Presenter is not OverlappedPresenter presenter)
        {
            throw new InvalidOperationException("Window pinning is only available in the overlapped presenter.");
        }

        TimerWindowPinBehavior behavior = TimerWindowPinPolicy.Resolve(pin, IsWindowMaximized);
        presenter.IsResizable = behavior.IsResizable;
        presenter.IsMaximizable = behavior.IsMaximizable;
        presenter.IsAlwaysOnTop = behavior.IsAlwaysOnTop;
        IsWindowPinned = pin;
    }

    /// <summary>Bumps <see cref="MainView.ThemeStamp"/> so descendant controls re-resolve their theme colors.</summary>
    public void NotifyThemeChanged() => ThemeStamp++;

    /// <summary>Persists and applies a theme choice through the window's live theme rail.</summary>
    /// <param name="theme">The new theme preference.</param>
    public void ApplyTheme(TimerThemeMode theme)
    {
        _store.UpdateTheme(theme);
        ApplyThemeCore(theme);
    }

    /// <summary>Persists and applies the canonical window font family without remounting the content tree.</summary>
    /// <param name="fontFamily">The selected family.</param>
    public void ApplyFontFamily(string fontFamily)
    {
        _store.UpdateFontFamily(fontFamily);
        ApplyFontFamilyCore(fontFamily);
    }

    /// <summary>Persists and applies the window background/material choice.</summary>
    /// <param name="background">The selected background mode.</param>
    public void ApplyBackground(TimerBackgroundMode background)
    {
        _store.UpdateBackground(background);
        ApplyBackgroundCore(background);
    }

    /// <summary>
    /// Persists the shell layout choice and closes any open side panel. The panel must go at once (not through its
    /// exit transition) because leaving the flyout layout also removes the footer command it was launched from — a
    /// lingering panel would outlive its own entry point.
    /// </summary>
    /// <param name="layoutMode">The selected layout mode.</param>
    public void ApplyLayoutMode(TimerLayoutMode layoutMode)
    {
        CloseSidePanelImmediately();
        _store.UpdateLayoutMode(layoutMode);
    }

    /// <summary>Sets the window caption from the current-language <see cref="Resources.AppName"/> (call after a language switch).</summary>
    public void SyncWindowTitle() => _window.Title = Resources.AppName;

    /// <inheritdoc/>
    void ILanguageApplyTarget.SetCurrentLanguage(string language)
    {
        Localizer.CurrentLanguage = language;
        LanguageApply.PinThreadCulture(language);
    }

    /// <inheritdoc/>
    void ILanguageApplyTarget.SyncWindowTitle() => SyncWindowTitle();

    /// <inheritdoc/>
    void ILanguageApplyTarget.BumpThemeRevision() => NotifyThemeChanged();

    /// <summary>
    /// Applies a language switch everywhere in one place: it persists the choice to the store, then applies the shared
    /// language-apply core (localizer + window title + theme-revision bump). Persisting is the only step unique to a
    /// runtime switch; the persisted-load path applies the same core without re-persisting.
    /// </summary>
    /// <param name="language">The BCP-47 language tag to switch to.</param>
    public void ApplyLanguage(string language)
    {
        _store.UpdateLanguage(language);
        ApplyLanguageCore(language);
    }

    /// <summary>
    /// Applies the language everywhere <b>except</b> persistence — the shared core of the runtime switch and the
    /// persisted-load path. The stored preference may be a concrete tag or the follow-system sentinel; it is resolved
    /// to the effective concrete language here (against the OS preferred language) before applying. Routing both paths
    /// through <see cref="LanguageApply.Apply"/> guarantees the theme-revision bump can never be dropped from one path.
    /// </summary>
    /// <param name="preferenceLanguage">The stored language preference (a concrete tag or the follow-system sentinel).</param>
    private void ApplyLanguageCore(string preferenceLanguage) =>
        LanguageApply.Apply(this, LanguageResolver.ResolveEffective(preferenceLanguage, OsCulture()));

    /// <summary>
    /// Re-resolves and re-applies the language when the OS preferred UI language changes at runtime — but only while the
    /// user's preference is "follow system"; a pinned concrete language is left untouched. The OS value is read from
    /// <see cref="Application.SystemLanguage"/> (authoritative), so it is correct even though the app has pinned the
    /// thread culture to the previously-applied language.
    /// </summary>
    public void OnSystemLanguageChanged()
    {
        if (LanguageResolver.IsFollowSystem(_store.Preferences.Language))
        {
            ApplyLanguageCore(TimerPreferences.SystemLanguage);
        }
    }

    // The OS preferred UI language as a culture, read from the framework's authoritative, uncached SystemLanguage
    // (not CultureInfo.CurrentUICulture, which the app pins to the applied language and which is the Windows *display*
    // language, not the preferred list). Falls back to CurrentUICulture if the framework value is unavailable (e.g. a
    // headless test with no running Application). Internal so App.OnLaunched can resolve the startup language against
    // the same source before any MainView instance exists.
    internal static System.Globalization.CultureInfo OsCulture()
    {
        try
        {
            string tag = Application.Current.SystemLanguage;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                return System.Globalization.CultureInfo.GetCultureInfo(tag);
            }
        }
        catch (Exception exception)
            when (exception is InvalidOperationException or System.Globalization.CultureNotFoundException)
        {
            // No running Application (headless test) or an unrecognized tag — fall back below.
        }

        return System.Globalization.CultureInfo.CurrentUICulture;
    }

    /// <summary>Applies all persisted presentation preferences once the workspace has loaded, then announces readiness.</summary>
    public void OnDataLoaded()
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        SaveErrorMessage = string.Empty;
        TimerPreferences preferences = _store.Preferences;
        ApplyLanguageCore(preferences.Language);
        ApplyThemeCore(preferences.Theme);
        ApplyFontFamilyCore(preferences.FontFamily);
        ApplyBackgroundCore(preferences.Background);
        ShowStartupNotices();
    }

    private void ApplyThemeCore(TimerThemeMode theme) =>
        _window.RequestedTheme = theme switch
        {
            TimerThemeMode.Light => ThemeRequest.Light,
            TimerThemeMode.Dark => ThemeRequest.Dark,
            _ => null,
        };

    private void ApplyFontFamilyCore(string fontFamily) =>
        _window.DefaultFontFamilyOverride = fontFamily;

    private void ApplyBackgroundCore(TimerBackgroundMode background) =>
        _windowBackground.Apply(background);

    /// <summary>Shows a blocking save-failure message over the shell with a retry affordance.</summary>
    /// <param name="message">The failure message to display.</param>
    public void ShowSaveError(string message) => SaveErrorMessage = message;

    /// <summary>Updates the in-progress session title being edited.</summary>
    /// <param name="title">The new title text.</param>
    public void SetSessionTitle(string title) => SessionTitle = title ?? string.Empty;

    /// <summary>Selects a folder as current and announces the change.</summary>
    /// <param name="folderId">The folder to select.</param>
    public void SelectFolder(Guid folderId)
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        _store.SelectFolder(folderId);
    }

    /// <summary>Starts the timer if stopped, or stops it and files the session (using a default title when blank).</summary>
    public void StartOrStop()
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        if (_store.IsRunning)
        {
            string title = string.IsNullOrWhiteSpace(SessionTitle)
                ? Resources.UntitledSession
                : SessionTitle.Trim();
            TimerEntry entry = _store.StopTimer(title);
            SessionTitle = string.Empty;
            string clock = TimerFormat.Clock(TimeSpan.FromSeconds(entry.DurationSeconds));
            Announce(FormatOne(Resources.TimerStoppedAnnouncement, clock));
        }
        else
        {
            _store.StartTimer();
            Announce(Resources.TimerStartedAnnouncement);
        }
    }

    /// <summary>Toggles the compact overlay presenter (an always-on-top mini timer) on or off.</summary>
    public void ToggleCompact()
    {
        if (IsCompactPresenter)
        {
            WindowPlacementCoordinator.SaveCurrent(_window);
            _window.SetPresenter(WindowPresenterKind.Overlapped);
            return;
        }

        WindowPlacementCoordinator.SaveCurrent(_window);
        CompactOverlayPresenter presenter = CompactOverlayPresenter.Create();
        presenter.InitialSize = TimerCompactLayoutResolver.DefaultInitialSize;

        // Enforce the overlay's own minimum size so the user cannot shrink the picture-in-picture window into the range
        // where the tight clock would collide with the primary action (the resolver owns that no-overlap contract).
        presenter.MinSize = TimerCompactLayoutResolver.MinimumClientSize;
        presenter.MaxSize = TimerCompactLayoutResolver.MaximumClientSize;
        _window.SetPresenter(presenter);
        WindowPlacementCoordinator.Restore(_window, TimerWindowPlacementMode.CompactOverlay);
    }

    /// <summary>Retries the blocking operation: re-saves after a save failure, or reloads after a bootstrap error.</summary>
    public async void Retry()
    {
        if (SaveErrorMessage.Length > 0)
        {
            try
            {
                await _store.FlushSaveAsync().ConfigureAwait(true);
                _store.SaveState.Rethrow();
                SaveErrorMessage = string.Empty;
            }
            catch (Exception exception) when (
                exception is TimerDataException or IOException or UnauthorizedAccessException)
            {
                SaveErrorMessage = exception.Message;
            }

            return;
        }

        if (_store.Bootstrap.IsError)
        {
            try
            {
                await _store.LoadAsync().ConfigureAwait(true);
                OnDataLoaded();
            }
            catch (Exception exception) when (
                exception is TimerDataException
                    or LegacyDataMigrationException
                    or LegacyDataAccessException
                    or InvalidOperationException)
            {
                // The reactive Bootstrap error state is refreshed by the store; nothing more to surface here.
            }
        }
    }

    /// <summary>Announces a one-shot, fully localized message to assistive technology (never per-second updates).</summary>
    /// <param name="message">The message to announce.</param>
    internal void Announce(string message) =>
        _window.Announce(message, AutomationNotificationKind.ActionCompleted);

    private static string FormatOne(string format, string argument) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, format, argument);
}
