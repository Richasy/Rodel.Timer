using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;

using Sprout;
using Sprout.Graphics;
using Sprout.Input;
using Sprout.Theming;

namespace Rodel.Timer;

public sealed class App : Application
{
    private Window _window = null!;

    /// <inheritdoc/>
    protected override void OnLaunched(LaunchEventArgs e)
    {
        _window = OpenWindow(new WindowSpec
        {
            Title = "Rodel.Timer",
            Size = new Size(720f, 480f),
            SystemBackdrop = SystemBackdrop.Mica,
        });

        _window.KeyDown += OnKey;
        _window.ActualThemeChanged += (_, _) => SyncBackground();
        SyncBackground();

        var mainView = new MainView(_window);
        _window.SetContent(mainView);
        _window.Show();
        InitializeDataAsync(mainView);
    }

    /// <inheritdoc/>
    protected override void OnActivated(AppActivationArguments args)
    {
    }

    private void OnKey(KeyEvent key)
    {
        if (key.Repeat || key.Key != Key.T || (key.Modifiers & KeyModifiers.Control) == 0)
        {
            return;
        }

        _window.RequestedTheme = _window.RequestedTheme switch
        {
            null => ThemeRequest.Light,
            ThemeRequest.Light => ThemeRequest.Dark,
            _ => null,
        };
    }

    private async void InitializeDataAsync(MainView mainView)
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellation.Token;
        EventHandler closed = (_, _) => cancellation.Cancel();
        _window.Closed += closed;

        try
        {
            TimerDataBootstrapper bootstrapper =
                Services.GetRequiredService<TimerDataBootstrapper>();
            TimerBootstrapResult result = await bootstrapper
                .LoadAsync(cancellationToken)
                .ConfigureAwait(false);

            _ = DispatcherQueue.TryEnqueue(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _window.RequestedTheme = result.Workspace.Preferences.Theme switch
                {
                    TimerThemeMode.Light => ThemeRequest.Light,
                    TimerThemeMode.Dark => ThemeRequest.Dark,
                    _ => null,
                };
                mainView.ShowReady(result);
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is TimerDataException
                or LegacyDataMigrationException
                or LegacyDataAccessException)
        {
            string message =
                $"{exception.Message}\n\nYour original Work-Timer data was left untouched; no replacement data was written.";
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    mainView.ShowDataError(message);
                }
            });
        }
        finally
        {
            _window.Closed -= closed;
        }
    }

    private void SyncBackground() => _window.Background = _window.Theme.Colors.SolidBackgroundFillColorBase;
}
