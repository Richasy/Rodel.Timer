namespace Rodel.Timer.Presentation;

/// <summary>The deterministic normal-workspace metrics resolved from client width and height.</summary>
/// <param name="ShowMiddleStatus">Whether the footer's running/material status column is present.</param>
/// <param name="ClockSize">The rolling-clock font size.</param>
/// <param name="AxisSegmentWidth">The width of each horizontal axis segment beside the clock.</param>
/// <param name="StageVerticalPadding">The timer stage's top/bottom padding.</param>
/// <param name="FooterVerticalPadding">The footer's top/bottom padding.</param>
public readonly record struct TimerNormalLayout(
    bool ShowMiddleStatus,
    float ClockSize,
    float AxisSegmentWidth,
    float StageVerticalPadding,
    float FooterVerticalPadding);

/// <summary>Resolves the open normal workspace without scaling the whole view.</summary>
public static class TimerNormalLayoutResolver
{
    /// <summary>The width below which the middle footer status column is removed.</summary>
    public const double NarrowThreshold = 720d;

    /// <summary>The height below which the stage and clock use their short-window metrics.</summary>
    public const double ShortThreshold = 560d;

    /// <summary>Resolves normal-workspace metrics from the effective-pixel client size.</summary>
    public static TimerNormalLayout Resolve(double width, double height)
    {
        bool shortWindow = height < ShortThreshold;
        float segment = (float)Math.Clamp((width - 400d) / 2d, 48d, 280d);
        return new(
            ShowMiddleStatus: width >= NarrowThreshold,
            ClockSize: shortWindow ? 58f : 96f,
            AxisSegmentWidth: segment,
            StageVerticalPadding: shortWindow ? 18f : 36f,
            FooterVerticalPadding: shortWindow ? 10f : 14f);
    }
}
