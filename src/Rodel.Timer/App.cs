using Rodel.Timer.AppData;
using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Backend.D2D;
using Sprout.Graphics;

namespace Rodel.Timer;

/// <summary>
/// The Rodel Timer application entry point. It opens the main window, constructs the reactive
/// <see cref="TimerAppStore"/> over the persistence services, hosts the product <see cref="MainView"/>, kicks off
/// the background bootstrap, and owns the save-on-close lifecycle (a deferred close that flushes pending saves and
/// keeps the window open on a save failure so the user can retry).
/// </summary>
public sealed class App : Application, IDisposable
{
    private Window _window = null!;
    private TimerAppStore _store = null!;
    private MainView _mainView = null!;
    private bool _forceClose;
    private bool _storeDisposed;

    /// <summary>
    /// Disposes the reactive store and the product view. Idempotent: the store is normally disposed in the
    /// save-on-close lifecycle and the window owns its content's disposal, so this is a defensive final release
    /// (the analyzer requires the owner of disposable fields to be disposable).
    /// </summary>
    public void Dispose()
    {
        DisposeStore();
        _mainView?.Dispose();
    }

    /// <inheritdoc/>
    protected override void OnLaunched(LaunchEventArgs e)
    {
        string? savedPlacement = WindowPlacementStore.Load();
        _window = OpenWindow(new WindowSpec
        {
            Title = Resources.AppName,
            Size = new Size(1100f, 720f),
            MinSize = new Size(480f, 460f),
            TitleBarHeight = TitleBarHeight.Tall,
            SystemBackdrop = SystemBackdrop.Mica,
            Placement = WindowPlacement.TryParse(savedPlacement, out WindowPlacement restored) ? restored : null,
        });

        TimerDataBootstrapper bootstrapper = Services.GetRequiredService<TimerDataBootstrapper>();
        TimerDataRepository repository = Services.GetRequiredService<TimerDataRepository>();
        TimeProvider timeProvider = Services.GetRequiredService<TimeProvider>();

        _store = new TimerAppStore(
            bootstrapper.LoadAsync,
            repository.SaveAsync,
            timeProvider,
            TimeZoneInfo.Local,
            new DispatcherQueueScheduler(DispatcherQueue));

        _mainView = new MainView(_window, _store, LoadFontChoices());

        _window.SizeChanged += OnSizeChanged;
        _window.PresenterChanged += OnPresenterChanged;
        _window.ActualThemeChanged += OnActualThemeChanged;
        _window.Closing += OnClosing;
        _window.Closed += OnClosed;
        SystemLanguageChanged += OnSystemLanguageChanged;

        SyncBackground();
        _window.SetContent(_mainView);
        _window.SetTitleBarContent(new TimerTitleBarControl(_mainView.Chrome));
        _window.Show();
        _mainView.SetViewportSize(_window.ClientSize.Width, _window.ClientSize.Height);

        LoadDataAsync();
    }

    /// <inheritdoc/>
    protected override void OnActivated(AppActivationArguments args)
    {
    }

    private async void LoadDataAsync()
    {
        try
        {
            await _store.LoadAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (
            exception is TimerDataException
                or LegacyDataMigrationException
                or LegacyDataAccessException
                or InvalidOperationException)
        {
            // The reactive Bootstrap state already carries the failure; MainView renders the error surface.
            return;
        }

        // The load task completes normally even on an internal error/cancel, and the window may have been closed (the
        // store disposed) while it was in flight. Guard on the disposed flag first (short-circuit so a disposed store is
        // never touched), then require a successful bootstrap before applying the loaded state.
        if (_storeDisposed || !AppLoadPolicy.ShouldApplyLoadedData(_storeDisposed, _store.Bootstrap.IsSuccess))
        {
            return;
        }

        _mainView.OnDataLoaded();
    }

    private void OnSizeChanged(object? sender, WindowSizeChangedEventArgs e) =>
        _mainView.SetViewportSize(e.Size.Width, e.Size.Height);

    private void OnPresenterChanged(object? sender, EventArgs e) =>
        _mainView.SetCompactPresenter(_window.Presenter.Kind == WindowPresenterKind.CompactOverlay);

    private void OnActualThemeChanged(object? sender, EventArgs e)
    {
        SyncBackground();
        _mainView.NotifyThemeChanged();
    }

    // The OS preferred UI language changed at runtime — let the view re-resolve + re-apply, but only while the user's
    // language preference is "follow system" (it no-ops for a pinned concrete language), mirroring the theme's
    // ActualThemeChanged flow.
    private void OnSystemLanguageChanged(object? sender, SystemLanguageChangedEventArgs e) =>
        _mainView.OnSystemLanguageChanged();

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Capture the window placement while the HWND still exists (the Closing event fires before teardown), so the
        // window reopens where the user left it. Saved on every pass — a deferred close that stays open on a save error
        // simply re-captures the latest placement on the next attempt.
        WindowPlacementStore.Save(_window.SavePlacement().Serialize());

        if (_forceClose)
        {
            return;
        }

        e.Cancel = true;

        try
        {
            await _store.CloseAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (
            exception is TimerDataException or IOException or UnauthorizedAccessException)
        {
            _mainView.ShowSaveError(exception.Message);
            return;
        }

        _forceClose = true;
        DisposeStore();
        _window.Close();
    }

    private void OnClosed(object? sender, EventArgs e) => DisposeStore();

    private void DisposeStore()
    {
        if (_storeDisposed)
        {
            return;
        }

        _storeDisposed = true;
        _store.Dispose();
    }

    private void SyncBackground() =>
        _window.Background = _window.Theme.Colors.SolidBackgroundFillColorBase;

    private static List<FontChoice> LoadFontChoices()
    {
        List<FontChoice> choices = [];
        foreach (SystemFontFamily family in SystemFontCatalog.GetFamilies())
        {
            choices.Add(new FontChoice(family.DisplayName, family.FamilyName));
        }

        return choices;
    }
}
