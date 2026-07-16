namespace Rodel.Timer.Core.Settings;

/// <summary>The persisted window-background preference, independent of the light/dark theme preference.</summary>
public enum TimerBackgroundMode
{
    /// <summary>Use the platform's long-lived main-window Mica material.</summary>
    Mica = 0,

    /// <summary>Use the higher-tint tabbed-window Mica variant.</summary>
    MicaAlt = 1,

    /// <summary>Use the platform's transient-window Acrylic material.</summary>
    Acrylic = 2,

    /// <summary>Use the theme-aware, more translucent Composition Acrylic variant.</summary>
    AcrylicThin = 3,

    /// <summary>Use the screen-recordable wallpaper-backed Liquid Glass material.</summary>
    LiquidGlassWallpaper = 4,
}
