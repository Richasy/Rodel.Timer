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
    /// The definite height for a folder/history list of <paramref name="rowCount"/> rows. It HUGS the content (no
    /// empty tail) until the content would exceed <see cref="MaximumListHeight"/>, then caps there so the ItemsView
    /// scroller activates: a definite height makes the composite measure TIGHT, and the OverlayPanel measures its
    /// child loosely against that maximum, so the inner ScrollView sees a bounded viewport &lt; content and scrolls.
    /// A bare MaxHeight cap does not achieve this — an unbounded measure leaves the viewport equal to the content.
    /// </summary>
    /// <param name="rowCount">The number of rows in the list (0 yields the empty-state minimum).</param>
    /// <returns>The definite list height in epx.</returns>
    public static float ListHeight(int rowCount) =>
        rowCount == 0
            ? MinimumListHeight
            : System.Math.Min((rowCount * RowStride) + ListPadding, MaximumListHeight);
}
