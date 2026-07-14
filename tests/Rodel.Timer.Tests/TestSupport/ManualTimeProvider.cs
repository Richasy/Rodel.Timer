namespace Rodel.Timer.Tests.TestSupport;

/// <summary>
/// A deterministic <see cref="TimeProvider"/> for headless store tests: a manually-advanced wall clock plus a
/// manually-advanced monotonic timestamp, kept as independent axes so a test can prove the running session's elapsed
/// time (which reads the timestamp) is immune to a wall-clock jump (NTP / DST / manual change). It is a self-contained
/// copy so the test project never references the Core test assembly.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;
    private long _timestamp;

    /// <summary>Creates the provider at <paramref name="start"/> with a zero monotonic timestamp.</summary>
    /// <param name="start">The initial wall-clock instant.</param>
    /// <param name="localTimeZone">The zone <see cref="LocalTimeZone"/> reports; defaults to <see cref="TimeZoneInfo.Utc"/>.</param>
    public ManualTimeProvider(DateTimeOffset start, TimeZoneInfo? localTimeZone = null)
    {
        _utcNow = start;
        LocalTimeZoneOverride = localTimeZone ?? TimeZoneInfo.Utc;
    }

    /// <summary>The zone reported by <see cref="LocalTimeZone"/>.</summary>
    public TimeZoneInfo LocalTimeZoneOverride { get; set; }

    /// <inheritdoc/>
    public override TimeZoneInfo LocalTimeZone => LocalTimeZoneOverride;

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <inheritdoc/>
    public override long GetTimestamp() => _timestamp;

    /// <summary>Advances both the wall clock and the monotonic timestamp by <paramref name="delta"/> (the normal passage of time).</summary>
    /// <param name="delta">The amount to advance; must not be negative.</param>
    public void Advance(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        _utcNow += delta;
        _timestamp += delta.Ticks;
    }

    /// <summary>
    /// Moves only the wall clock by <paramref name="delta"/> (which may be negative), leaving the monotonic timestamp
    /// untouched — models a clock adjustment that must not affect a running session's elapsed time.
    /// </summary>
    /// <param name="delta">The wall-clock shift; may be negative.</param>
    public void ShiftWallClock(TimeSpan delta) => _utcNow += delta;
}
