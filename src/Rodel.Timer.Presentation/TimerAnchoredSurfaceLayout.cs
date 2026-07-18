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

    /// <summary>The per-row stride inside the list: the row extent (50) plus the inter-row gap (4).</summary>
    private const float RowStride = 54f;

    /// <summary>The list's own top+bottom padding (8 + 8) minus the trailing gap the last row omits (4).</summary>
    private const float ListPadding = 12f;

    /// <summary>The minimum list height — holds the centered empty-state text when there are no rows.</summary>
    public const float MinimumListHeight = 80f;

    /// <summary>The maximum list height before it scrolls internally (keeps header + list within <see cref="MaximumHeight"/>).</summary>
    public const float MaximumListHeight = 320f;

    /// <summary>
    /// The DEFINITE list height for a folder/history list of <paramref name="rowCount"/> rows. The inner ScrollView is a
    /// viewport window that FILLS the height it is given, so this must be the EXACT content height while the list fits:
    /// the viewport then equals the content and <see cref="MaximumListHeight"/> scroll offset (content − viewport, in
    /// epx) is exactly zero, so the conscious scrollbar never reveals on hover for a list that fits (the round-11
    /// complaint) and there is no empty tail (the round-10 complaint — a bare MaxHeight cap fills to the cap instead).
    /// Past the cap the height stays pinned at <see cref="MaximumListHeight"/> so the viewport is smaller than the
    /// content and the list scrolls internally (the round-7 requirement).
    /// </summary>
    /// <param name="rowCount">The number of rows in the list (0 yields the empty-state minimum).</param>
    /// <returns>The definite list height in epx.</returns>
    public static float ListHeight(int rowCount) =>
        rowCount == 0
            ? MinimumListHeight
            : System.Math.Min((rowCount * RowStride) + ListPadding, MaximumListHeight);
}
