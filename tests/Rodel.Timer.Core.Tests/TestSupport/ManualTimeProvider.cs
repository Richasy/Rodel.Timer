namespace Rodel.Timer.Core.Tests.TestSupport;

internal sealed class ManualTimeProvider(
    DateTimeOffset utcNow,
    TimeZoneInfo? localTimeZone = null) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;
    private long _timestamp;

    public override TimeZoneInfo LocalTimeZone { get; } = localTimeZone ?? TimeZoneInfo.Utc;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override long GetTimestamp() => _timestamp;

    public void Advance(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        _utcNow += duration;
        _timestamp = checked(_timestamp + duration.Ticks);
    }

    public void ShiftWallClock(TimeSpan offset) => _utcNow += offset;
}
