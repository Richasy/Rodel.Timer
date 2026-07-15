using System.Globalization;

namespace Rodel.Timer.Presentation;

/// <summary>Pure, culture-invariant formatting for elapsed and accumulated timer durations.</summary>
public static class TimerFormat
{
    /// <summary>
    /// Formats a duration as a zero-padded <c>HH:mm:ss</c> clock (hours are not capped at 24). Negative
    /// durations are clamped to zero.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>The clock string, for example <c>01:05:03</c>.</returns>
    public static string Clock(TimeSpan duration)
    {
        long totalSeconds = duration <= TimeSpan.Zero ? 0L : (long)duration.TotalSeconds;
        long hours = totalSeconds / 3600L;
        long minutes = totalSeconds % 3600L / 60L;
        long seconds = totalSeconds % 60L;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{hours:D2}:{minutes:D2}:{seconds:D2}");
    }

    /// <summary>Formats a duration as whole minutes (rounded down), the unit used for the daily target summary.</summary>
    /// <param name="duration">The duration to convert.</param>
    /// <returns>The whole-minute count, never negative.</returns>
    public static int WholeMinutes(TimeSpan duration) =>
        duration <= TimeSpan.Zero ? 0 : (int)duration.TotalMinutes;
}
