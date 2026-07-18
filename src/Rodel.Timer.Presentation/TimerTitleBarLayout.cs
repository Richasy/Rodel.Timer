namespace Rodel.Timer.Presentation;

/// <summary>The deterministic responsive state of the extended title bar.</summary>
/// <param name="IsVisible">Whether the normal title-bar content is present.</param>
/// <param name="ShowBrandLabel">Whether the product name is visible beside the brand glyph.</param>
/// <param name="ShowCommandLabels">Whether Folders and History show labels and chevrons instead of icon-only chrome.</param>
public readonly record struct TimerTitleBarLayout(
    bool IsVisible,
    bool ShowBrandLabel,
    bool ShowCommandLabels);

/// <summary>Resolves extended-title-bar visibility from presenter mode and viewport width.</summary>
public static class TimerTitleBarLayoutResolver
{
    /// <summary>The width below which the brand and command labels collapse.</summary>
    public const double LabelThreshold = 720d;

    /// <summary>Resolves the title-bar state.</summary>
    /// <param name="viewportWidth">The window client width in effective pixels.</param>
    /// <param name="isCompact">Whether the compact-overlay presenter is active.</param>
    /// <returns>The title-bar visibility state.</returns>
    public static TimerTitleBarLayout Resolve(double viewportWidth, bool isCompact)
    {
        if (isCompact)
        {
            return new(IsVisible: false, ShowBrandLabel: false, ShowCommandLabels: false);
        }

        bool showLabels = viewportWidth >= LabelThreshold;
        return new(IsVisible: true, showLabels, showLabels);
    }
}
