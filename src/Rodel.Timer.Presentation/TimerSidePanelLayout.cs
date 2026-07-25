namespace Rodel.Timer.Presentation;

/// <summary>Which edge a side panel is docked to.</summary>
public enum TimerSidePanelSide
{
    /// <summary>The leading edge — the folders panel, launched from the footer's current-folder column.</summary>
    Left,

    /// <summary>The trailing edge — the history panel, launched from the footer's today's-focus column.</summary>
    Right,
}

/// <summary>
/// The resolved client-space footprint of a side panel: the rectangle it occupies and the horizontal distance it
/// travels while sliding in from its edge.
/// </summary>
/// <param name="Left">The panel's left edge in client-space effective pixels.</param>
/// <param name="Top">The panel's top edge in client-space effective pixels.</param>
/// <param name="Width">The panel's width in effective pixels.</param>
/// <param name="Height">The panel's height in effective pixels.</param>
/// <param name="SlideOffset">The paint-time X offset the panel enters from and exits to (negative for a left panel, positive for a right one).</param>
public readonly record struct TimerSidePanelLayout(
    float Left,
    float Top,
    float Width,
    float Height,
    float SlideOffset);

/// <summary>
/// Resolves where a side panel sits: a fixed-width surface inset <see cref="Inset"/> effective pixels inside the band
/// between the extended title bar and the footer. The whole placement is a pure function of the client size, the title
/// bar band, and the footer height, so it is asserted headlessly and the panel never has to measure the shell.
/// </summary>
public static class TimerSidePanelLayoutResolver
{
    /// <summary>The margin kept between the panel and each side of the title-bar-to-footer band (12 epx).</summary>
    public const float Inset = 12f;

    /// <summary>The panel's fixed width — the same footprint as the anchored Folders/History flyouts.</summary>
    public const float Width = TimerAnchoredSurfaceLayoutResolver.Width;

    /// <summary>The smallest panel height that still shows a usable list; below it the band is simply too short.</summary>
    public const float MinimumHeight = 160f;

    /// <summary>The extra distance past the panel's own width that the slide starts from, so its shadow clears the edge.</summary>
    private const float SlideOvershoot = 24f;

    /// <summary>
    /// Resolves the panel footprint for one side. The width is clamped so a very narrow window still leaves the
    /// <see cref="Inset"/> margin on both sides, and the height fills the band between the title bar and the footer.
    /// </summary>
    /// <param name="clientWidth">The window client width in effective pixels.</param>
    /// <param name="clientHeight">The window client height in effective pixels.</param>
    /// <param name="titleBarHeight">The extended title bar band height in effective pixels.</param>
    /// <param name="footerHeight">The footer height in effective pixels.</param>
    /// <param name="side">The edge the panel docks to.</param>
    /// <returns>The resolved footprint.</returns>
    public static TimerSidePanelLayout Resolve(
        double clientWidth,
        double clientHeight,
        float titleBarHeight,
        float footerHeight,
        TimerSidePanelSide side)
    {
        float width = (float)Math.Max(0d, Math.Min(Width, clientWidth - (2d * Inset)));
        float top = titleBarHeight + Inset;
        float bottom = (float)clientHeight - footerHeight - Inset;
        float height = Math.Max(0f, bottom - top);

        float left = side == TimerSidePanelSide.Left
            ? Inset
            : (float)clientWidth - Inset - width;

        float slide = width + Inset + SlideOvershoot;
        return new TimerSidePanelLayout(
            left,
            top,
            width,
            height,
            side == TimerSidePanelSide.Left ? -slide : slide);
    }

    /// <summary>
    /// Whether the band between the title bar and the footer is tall and wide enough to host a panel. A window shorter
    /// than this simply keeps its footer commands inert rather than opening a clipped, unusable surface.
    /// </summary>
    /// <param name="clientWidth">The window client width in effective pixels.</param>
    /// <param name="clientHeight">The window client height in effective pixels.</param>
    /// <param name="titleBarHeight">The extended title bar band height in effective pixels.</param>
    /// <param name="footerHeight">The footer height in effective pixels.</param>
    /// <returns><see langword="true"/> when a panel fits.</returns>
    public static bool CanShow(double clientWidth, double clientHeight, float titleBarHeight, float footerHeight)
    {
        TimerSidePanelLayout layout = Resolve(
            clientWidth,
            clientHeight,
            titleBarHeight,
            footerHeight,
            TimerSidePanelSide.Left);
        return layout.Width > 0f && layout.Height >= MinimumHeight;
    }
}
