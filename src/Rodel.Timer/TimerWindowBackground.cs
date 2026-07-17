using Rodel.Timer.Core.Settings;

using Sprout;
using Sprout.Graphics;
using Sprout.Theming;

namespace Rodel.Timer;

/// <summary>
/// Owns the window's background plane and applies one persisted mode without exposing compositor details to settings.
/// Theme-aware material recipes always use the window palette's opaque base surface as their policy fallback. The
/// compact overlay's opaque surface is handled by the framework: a topmost (compact-overlay / always-on-top) window
/// suppresses its window material to the opaque themed fallback automatically and restores it on return to the
/// overlapped form, so this owner just applies the persisted material for every presenter.
/// </summary>
internal sealed class TimerWindowBackground
{
    private readonly Window _window;
    private readonly WindowBackdropMaterial _acrylicThin;
    private TimerBackgroundMode _requestedMode = TimerBackgroundMode.Mica;

    /// <summary>Creates the background owner for <paramref name="window"/>.</summary>
    /// <param name="window">The product window.</param>
    internal TimerWindowBackground(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _acrylicThin = WindowBackdropMaterial.Custom(ResolveAcrylicThin);
    }

    /// <summary>Persists and applies <paramref name="mode"/> live for the current presenter.</summary>
    /// <param name="mode">The persisted background choice.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a defined mode.</exception>
    internal void Apply(TimerBackgroundMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        _requestedMode = mode;
        Refresh();
    }

    /// <summary>Resolves the post-apply active/fallback state for status presentation (normal presenter only).</summary>
    /// <param name="mode">The persisted background choice.</param>
    /// <returns>The effective background resolution.</returns>
    internal TimerBackgroundResolution Resolve(TimerBackgroundMode mode) =>
        TimerBackgroundResolution.Resolve(mode, _window.IsBackdropActive);

    private void Refresh()
    {
        _window.Background = FallbackColor;

        switch (_requestedMode)
        {
            case TimerBackgroundMode.Mica:
                ApplySystemBackdrop(SystemBackdrop.Mica);
                break;
            case TimerBackgroundMode.MicaAlt:
                ApplySystemBackdrop(SystemBackdrop.MicaAlt);
                break;
            case TimerBackgroundMode.Acrylic:
                ApplySystemBackdrop(SystemBackdrop.Acrylic);
                break;
            case TimerBackgroundMode.AcrylicThin:
                ApplyMaterial(_acrylicThin);
                break;
            default:
                throw new InvalidOperationException("The requested background mode is not supported.");
        }
    }

    private Color FallbackColor =>
        _window.Theme.Colors.SolidBackgroundFillColorBase with { A = byte.MaxValue };

    private AcrylicMaterial ResolveAcrylicThin(ThemeMode mode) =>
        AcrylicMaterial.Thin(mode == ThemeMode.Dark) with { FallbackColor = FallbackColor };

    private void ApplyMaterial(WindowBackdropMaterial material)
    {
        _window.BackdropMaterial = material;
        _window.SystemBackdrop = SystemBackdrop.None;
    }

    private void ApplySystemBackdrop(SystemBackdrop backdrop)
    {
        _window.SystemBackdrop = backdrop;
        _window.BackdropMaterial = null;
    }
}
