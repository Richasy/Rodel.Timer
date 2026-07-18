using Sprout.Graphics;

namespace Rodel.Timer.Presentation;

/// <summary>The finite compact-overlay content states.</summary>
public enum TimerCompactMode
{
    /// <summary>Clock and primary action share one row; context is absent.</summary>
    Tight,

    /// <summary>Context and clock share a leading baseline while the action spans both rows.</summary>
    Balanced,

    /// <summary>The balanced two-column form with a wider, capped content band.</summary>
    Wide,
}

/// <summary>The deterministic compact-overlay metrics resolved from client width and height.</summary>
public readonly record struct TimerCompactLayout(
    TimerCompactMode Mode,
    bool ShowContext,
    float ClockSize,
    float ActionSize,
    float ColumnGap,
    float HorizontalPadding,
    float OuterHorizontalPadding,
    float ContentMaxWidth);

/// <summary>Resolves one of the three approved compact-overlay layouts without whole-view scaling.</summary>
public static class TimerCompactLayoutResolver
{
    /// <summary>Widths below this value use Tight.</summary>
    public const double TightWidthThreshold = 320d;

    /// <summary>Heights below this value use Tight.</summary>
    public const double TightHeightThreshold = 158d;

    /// <summary>Widths at or above this value may use Wide.</summary>
    public const double WideWidthThreshold = 420d;

    /// <summary>Wide applies only below this height.</summary>
    public const double WideHeightThreshold = 220d;

    /// <summary>The capped inner content band all three states share.</summary>
    public const float ContentBandMaxWidth = 390f;

    /// <summary>The outer padding floor at small widths (each side).</summary>
    public const float MinimumOuterHorizontalPadding = 14f;

    /// <summary>The Tight clock font size.</summary>
    public const float TightClockSize = 31f;

    /// <summary>The Tight primary-action diameter.</summary>
    public const float TightActionSize = 40f;

    /// <summary>The Tight column gap between the clock cell and the action.</summary>
    public const float TightColumnGap = 16f;

    /// <summary>The Tight grid's own horizontal padding (each side).</summary>
    public const float TightHorizontalPadding = 8f;

    /// <summary>The glyph count of the "HH:MM:SS" clock.</summary>
    public const int ClockGlyphCount = 8;

    /// <summary>
    /// A deliberately generous per-glyph advance (as a fraction of the font size) used to bound the clock width. Segoe
    /// UI Variable tabular digits advance about 0.5 em and a colon about 0.27 em; 0.62 em per glyph is a safe upper
    /// bound so the pure no-overlap contract never under-estimates the real DirectWrite width.
    /// </summary>
    public const float ClockGlyphAdvanceFactor = 0.62f;

    /// <summary>
    /// The compact-overlay window's minimum client size (epx). It is set on the presenter so the user cannot shrink the
    /// picture-in-picture window into the size where the tight clock would collide with the primary action; the pure
    /// no-overlap contract below is verified against it. Matches the design prototype's smallest tight point.
    /// </summary>
    public static Size MinimumClientSize { get; } = new(300f, 150f);

    /// <summary>The compact-overlay window's initial client size (epx) — the balanced design point.</summary>
    public static Size DefaultInitialSize { get; } = new(360f, 203f);

    /// <summary>A conservative upper bound for the "HH:MM:SS" clock width at a given font size.</summary>
    /// <param name="clockFontSize">The clock font size in effective pixels.</param>
    /// <returns>The estimated maximum clock width in effective pixels.</returns>
    public static float EstimateClockWidth(float clockFontSize) =>
        ClockGlyphCount * ClockGlyphAdvanceFactor * clockFontSize;

    /// <summary>
    /// The horizontal room the clock cell gets in a resolved layout at a given client width — the client width less both
    /// outer paddings, both grid paddings, the column gap, and the action column. The clock never overlaps the action
    /// while this stays at or above <see cref="EstimateClockWidth"/> of the layout's clock size.
    /// </summary>
    /// <param name="layout">The resolved layout.</param>
    /// <param name="clientWidth">The client width in effective pixels.</param>
    /// <returns>The clock cell's available width in effective pixels.</returns>
    public static float ClockAvailableWidth(TimerCompactLayout layout, double clientWidth) =>
        (float)clientWidth
        - (2f * layout.OuterHorizontalPadding)
        - (2f * layout.HorizontalPadding)
        - layout.ColumnGap
        - layout.ActionSize;

    /// <summary>Resolves a compact-overlay layout from its effective-pixel client size.</summary>
    public static TimerCompactLayout Resolve(double width, double height)
    {
        float outerHorizontalPadding =
            (float)Math.Max(MinimumOuterHorizontalPadding, (width - ContentBandMaxWidth) / 2d);

        if (width < TightWidthThreshold || height < TightHeightThreshold)
        {
            return new(
                TimerCompactMode.Tight,
                ShowContext: false,
                ClockSize: TightClockSize,
                ActionSize: TightActionSize,
                ColumnGap: TightColumnGap,
                HorizontalPadding: TightHorizontalPadding,
                OuterHorizontalPadding: outerHorizontalPadding,
                ContentMaxWidth: ContentBandMaxWidth);
        }

        if (width >= WideWidthThreshold && height < WideHeightThreshold)
        {
            return new(
                TimerCompactMode.Wide,
                ShowContext: true,
                ClockSize: 42f,
                ActionSize: 48f,
                ColumnGap: 34f,
                HorizontalPadding: 0f,
                OuterHorizontalPadding: outerHorizontalPadding,
                ContentMaxWidth: ContentBandMaxWidth);
        }

        return new(
            TimerCompactMode.Balanced,
            ShowContext: true,
            ClockSize: 40f,
            ActionSize: 48f,
            ColumnGap: 22f,
            HorizontalPadding: 10f,
            OuterHorizontalPadding: outerHorizontalPadding,
            ContentMaxWidth: ContentBandMaxWidth);
    }
}
