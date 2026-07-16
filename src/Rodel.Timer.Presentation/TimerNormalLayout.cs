namespace Rodel.Timer.Presentation;

/// <summary>The deterministic normal-workspace metrics resolved from client width and height.</summary>
/// <param name="ShowMiddleStatus">Whether the footer''s running/material status column is present.</param>
/// <param name="ClockSize">The rolling-clock font size (continuous, like the prototype''s <c>clamp(84, 10vw, 116)</c>).</param>
/// <param name="AxisTotalWidth">The full width of the decorative axis line behind the clock.</param>
/// <param name="AxisGapWidth">The centre gap in the axis line (a little wider than the clock, so the flanking segments never cross the digits) — also the width of the soft backlight behind the clock.</param>
/// <param name="ShowAxisDecoration">Whether the axis line + backlight are shown (hidden on a narrow window so the clock stays centred).</param>
/// <param name="StageVerticalPadding">The timer stage''s top/bottom padding.</param>
/// <param name="FooterVerticalPadding">The footer''s top/bottom padding.</param>
public readonly record struct TimerNormalLayout(
    bool ShowMiddleStatus,
    float ClockSize,
    float AxisTotalWidth,
    float AxisGapWidth,
    bool ShowAxisDecoration,
    float StageVerticalPadding,
    float FooterVerticalPadding);

/// <summary>Resolves the open normal workspace without scaling the whole view.</summary>
public static class TimerNormalLayoutResolver
{
    /// <summary>The width below which the middle footer status column and the axis decoration are removed.</summary>
    public const double NarrowThreshold = 720d;

    /// <summary>The height below which the stage and clock use their short-window metrics.</summary>
    public const double ShortThreshold = 560d;

    /// <summary>Resolves normal-workspace metrics from the effective-pixel client size.</summary>
    public static TimerNormalLayout Resolve(double width, double height)
    {
        bool shortWindow = height < ShortThreshold;

        // A heavy, continuous clock size — the prototype''s clamp(84px, 10vw, 116px); a short window uses the compact 58.
        float clock = shortWindow ? 58f : (float)Math.Clamp(width * 0.10d, 84d, 116d);

        // The decorative axis line spans min(880, 86% of the client). Its centre gap is a little wider than the clock
        // (~"00:00:00" is roughly 4.3x the digit size in the tabular UI font) so the flanking segments never run under
        // the digits, and the same width sizes the soft backlight behind the clock.
        float axisTotal = (float)Math.Min(880d, width * 0.86d);
        float gap = MathF.Min(axisTotal, clock * 4.3f);

        return new(
            ShowMiddleStatus: width >= NarrowThreshold,
            ClockSize: clock,
            AxisTotalWidth: axisTotal,
            AxisGapWidth: gap,
            ShowAxisDecoration: width >= NarrowThreshold,
            StageVerticalPadding: shortWindow ? 18f : 36f,
            FooterVerticalPadding: shortWindow ? 10f : 14f);
    }
}
