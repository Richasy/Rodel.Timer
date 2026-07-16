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

    /// <summary>Resolves a compact-overlay layout from its effective-pixel client size.</summary>
    public static TimerCompactLayout Resolve(double width, double height)
    {
        float outerHorizontalPadding = (float)Math.Max(14d, (width - 390d) / 2d);

        if (width < TightWidthThreshold || height < TightHeightThreshold)
        {
            return new(
                TimerCompactMode.Tight,
                ShowContext: false,
                ClockSize: 31f,
                ActionSize: 40f,
                ColumnGap: 16f,
                HorizontalPadding: 8f,
                OuterHorizontalPadding: outerHorizontalPadding,
                ContentMaxWidth: 390f);
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
                ContentMaxWidth: 390f);
        }

        return new(
            TimerCompactMode.Balanced,
            ShowContext: true,
            ClockSize: 40f,
            ActionSize: 48f,
            ColumnGap: 22f,
            HorizontalPadding: 10f,
            OuterHorizontalPadding: outerHorizontalPadding,
            ContentMaxWidth: 390f);
    }
}
