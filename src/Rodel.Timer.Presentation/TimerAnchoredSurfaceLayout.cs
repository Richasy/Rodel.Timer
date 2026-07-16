namespace Rodel.Timer.Presentation;

/// <summary>
/// The shared footprint of a Folders or History anchored surface. The width is fixed; the height is left to the
/// framework flyout, which hugs the content and caps it at <see cref="MaximumHeight"/> (the list then scrolls) — the
/// WinUI-faithful "content-sized surface with a max, not a pinned box" model.
/// </summary>
public static class TimerAnchoredSurfaceLayoutResolver
{
    /// <summary>The shared anchored-surface width.</summary>
    public const float Width = 336f;

    /// <summary>The maximum surface height before the inner list scrolls.</summary>
    public const float MaximumHeight = 420f;
}
