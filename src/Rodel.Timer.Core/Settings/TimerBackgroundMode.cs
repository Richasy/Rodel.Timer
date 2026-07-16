namespace Rodel.Timer.Core.Settings;

/// <summary>The persisted window-background preference, independent of the light/dark theme preference.</summary>
public enum TimerBackgroundMode
{
    /// <summary>Use the platform's long-lived main-window material (Mica) with an opaque themed fallback.</summary>
    SystemMaterial,

    /// <summary>Use only the opaque themed solid background.</summary>
    Solid,

    /// <summary>Use a theme-aware window Acrylic material with an opaque themed fallback.</summary>
    Acrylic,

    /// <summary>Use the screen-recordable wallpaper-backed Liquid Glass material with an opaque themed fallback.</summary>
    LiquidGlassWallpaper,
}
