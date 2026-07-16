using Rodel.Timer.Core.Settings;

using Sprout;
using Sprout.Graphics;
using Sprout.LiquidGlass;
using Sprout.Theming;

namespace Rodel.Timer;

/// <summary>
/// Owns the window's background plane and applies one persisted mode without exposing compositor details to settings.
/// Theme-aware material recipes always use the window palette's opaque base surface as their policy fallback.
/// </summary>
internal sealed class TimerWindowBackground
{
    private readonly Window _window;
    private readonly WindowBackdropMaterial _acrylic;
    private readonly WindowBackdropMaterial _wallpaperGlass;

    /// <summary>Creates the background owner for <paramref name="window"/>.</summary>
    /// <param name="window">The product window.</param>
    internal TimerWindowBackground(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _acrylic = WindowBackdropMaterial.Custom(ResolveAcrylic);
        _wallpaperGlass = WindowBackdropMaterial.WallpaperGlass(ResolveWallpaperGlass);
    }

    /// <summary>Applies <paramref name="mode"/> live, preserving a material during cross-material handoffs.</summary>
    /// <param name="mode">The persisted background choice.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a defined mode.</exception>
    internal void Apply(TimerBackgroundMode mode)
    {
        _window.Background = FallbackColor;

        switch (mode)
        {
            case TimerBackgroundMode.SystemMaterial:
                ApplySystemBackdrop(SystemBackdrop.Mica);
                break;
            case TimerBackgroundMode.Solid:
                ApplySystemBackdrop(SystemBackdrop.None);
                break;
            case TimerBackgroundMode.Acrylic:
                ApplyMaterial(_acrylic);
                break;
            case TimerBackgroundMode.LiquidGlassWallpaper:
                ApplyMaterial(_wallpaperGlass);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    /// <summary>Resolves the post-apply active/fallback state for status presentation.</summary>
    /// <param name="mode">The persisted background choice.</param>
    /// <returns>The effective background resolution.</returns>
    internal TimerBackgroundResolution Resolve(TimerBackgroundMode mode) =>
        TimerBackgroundResolution.Resolve(mode, _window.IsBackdropActive);

    private Color FallbackColor =>
        _window.Theme.Colors.SolidBackgroundFillColorBase with { A = byte.MaxValue };

    private AcrylicMaterial ResolveAcrylic(ThemeMode mode) =>
        AcrylicMaterial.Thin(mode == ThemeMode.Dark) with { FallbackColor = FallbackColor };

    private LiquidGlassMaterial ResolveWallpaperGlass(ThemeMode mode) =>
        LiquidGlassMaterial.Regular(mode == ThemeMode.Dark) with { FallbackColor = FallbackColor };

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
