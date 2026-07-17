namespace Rodel.Timer.Presentation;

/// <summary>The deterministic normal-workspace metrics resolved from client width and height.</summary>
/// <param name="ShowMiddleStatus">Whether the footer''s running/material status column is present.</param>
/// <param name="ClockSize">The rolling-clock font size (continuous, like the prototype''s <c>clamp(84, 10vw, 116)</c>).</param>
/// <param name="AxisTotalWidth">The full width of the decorative axis line behind the clock (its fade + centre gap live in the gradient stops).</param>
/// <param name="GlowWidth">The width of the soft radial backlight behind the clock (the prototype''s <c>min(620, 72%)</c>).</param>
/// <param name="GlowHeight">The height of the soft radial backlight ellipse behind the clock.</param>
/// <param name="ShowAxisDecoration">Whether the axis line is shown (hidden on a narrow window so the clock stays centred).</param>
/// <param name="StageVerticalPadding">The timer stage''s top/bottom padding.</param>
/// <param name="FooterVerticalPadding">The footer''s top/bottom padding.</param>
public readonly record struct TimerNormalLayout(
    bool ShowMiddleStatus,
    float ClockSize,
    float AxisTotalWidth,
    float GlowWidth,
    float GlowHeight,
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

        // The decorative axis line spans min(880, 86% of the client); its inward fade and the centre gap for the clock
        // live in the gradient stops, so only its total width is metric. The soft backlight ellipse behind the clock is
        // a little wider than the prototype''s min(620, 72%) and taller (a more diffuse halo), and it does NOT inflate
        // the clock band (the view offsets it out with a negative pad), so a generous size is safe.
        float axisTotal = (float)Math.Min(880d, width * 0.86d);
        float glowWidth = (float)Math.Min(700d, width * 0.78d);
        float glowHeight = shortWindow ? 190f : 320f;

        return new(
            ShowMiddleStatus: width >= NarrowThreshold,
            ClockSize: clock,
            AxisTotalWidth: axisTotal,
            GlowWidth: glowWidth,
            GlowHeight: glowHeight,
            ShowAxisDecoration: width >= NarrowThreshold,
            StageVerticalPadding: shortWindow ? 18f : 36f,
            FooterVerticalPadding: shortWindow ? 10f : 14f);
    }
}
