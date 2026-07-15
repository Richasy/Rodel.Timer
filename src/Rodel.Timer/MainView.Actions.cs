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
    /// <summary>Updates the viewport width used for the responsive pane breakpoints.</summary>
    /// <param name="width">The new viewport width in effective pixels.</param>
    public void SetViewportWidth(double width) => ViewportWidth = width;

    /// <summary>Records the current compact-overlay presenter state (closes both panes when entering compact).</summary>
    /// <param name="isCompact">Whether the window is now in the compact overlay presenter.</param>
    public void SetCompactPresenter(bool isCompact)
    {
        IsCompactPresenter = isCompact;

        // Collapse the extended title bar to the standard 32 epx band while compact (its brand + command band is also
        // torn out of all three trees by the title bar's When(!IsCompact) switch), and restore the 48 epx tall band on
        // exit so the brand + settings + compact commands return.
        _window.TitleBarHeight = isCompact ? TitleBarHeight.Standard : TitleBarHeight.Tall;

        if (isCompact)
        {
            FolderPaneOpen = false;
            HistoryPaneOpen = false;
        }
    }

    /// <summary>Bumps <see cref="MainView.ThemeStamp"/> so descendant controls re-resolve their theme colors.</summary>
    public void NotifyThemeChanged() => ThemeStamp++;

    /// <summary>Sets the window caption from the current-language <see cref="Resources.AppName"/> (call after a language switch).</summary>
    public void SyncWindowTitle() => _window.Title = Resources.AppName;

    /// <inheritdoc/>
    void ILanguageApplyTarget.SetCurrentLanguage(string language) => Localizer.CurrentLanguage = language;

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
    /// persisted-load path. Routing both through <see cref="LanguageApply.Apply"/> guarantees the theme-revision bump
    /// (which re-values the already-mounted, same-key rows into the new language) can never be dropped from one path.
    /// </summary>
    /// <param name="language">The BCP-47 language tag to apply.</param>
    private void ApplyLanguageCore(string language) => LanguageApply.Apply(this, language);

    /// <summary>Applies the persisted language and theme once the workspace has loaded, then announces readiness.</summary>
    public void OnDataLoaded()
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        SaveErrorMessage = string.Empty;
        TimerPreferences preferences = _store.Preferences;
        ApplyLanguageCore(preferences.Language);
        _window.RequestedTheme = preferences.Theme switch
        {
            TimerThemeMode.Light => ThemeRequest.Light,
            TimerThemeMode.Dark => ThemeRequest.Dark,
            _ => null,
        };
        ShowStartupNotices();
    }

    /// <summary>Shows a blocking save-failure message over the shell with a retry affordance.</summary>
    /// <param name="message">The failure message to display.</param>
    public void ShowSaveError(string message) => SaveErrorMessage = message;

    /// <summary>Opens or closes the folder pane (controlled by the split view); closes the history pane if they are mutually exclusive.</summary>
    /// <param name="open">Whether the pane should be open.</param>
    public void SetFolderPaneOpen(bool open)
    {
        FolderPaneOpen = open;
        if (open && PanesAreMutuallyExclusive)
        {
            HistoryPaneOpen = false;
        }
    }

    /// <summary>Opens or closes the history pane (controlled by the split view); closes the folder pane if they are mutually exclusive.</summary>
    /// <param name="open">Whether the pane should be open.</param>
    public void SetHistoryPaneOpen(bool open)
    {
        HistoryPaneOpen = open;
        if (open && PanesAreMutuallyExclusive)
        {
            FolderPaneOpen = false;
        }
    }

    /// <summary>Toggles the folder pane open state (closing the history pane when the two are mutually exclusive).</summary>
    public void ToggleFolderPane() => SetFolderPaneOpen(!FolderPaneOpen);

    /// <summary>Toggles the history pane open state (closing the folder pane when the two are mutually exclusive).</summary>
    public void ToggleHistoryPane() => SetHistoryPaneOpen(!HistoryPaneOpen);

    /// <summary>Whether the two panes cannot be open at once at the current viewport (both overlay), so opening one closes the other.</summary>
    private bool PanesAreMutuallyExclusive =>
        TimerShell.PanesAreMutuallyExclusive(ViewportWidth, IsCompactPresenter);

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

    /// <summary>Toggles the compact overlay presenter (a 360×203 always-on-top mini timer) on or off.</summary>
    public void ToggleCompact()
    {
        if (IsCompactPresenter)
        {
            _window.SetPresenter(WindowPresenterKind.Overlapped);
            return;
        }

        CompactOverlayPresenter presenter = CompactOverlayPresenter.Create();
        presenter.InitialSize = new Sprout.Graphics.Size(360f, 203f);
        _window.SetPresenter(presenter);
    }

    /// <summary>Retries the blocking operation: re-saves after a save failure, or reloads after a bootstrap error.</summary>
    public async void Retry()
    {
        if (SaveErrorMessage.Length > 0)
        {
            try
            {
                await _store.FlushSaveAsync().ConfigureAwait(true);
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
