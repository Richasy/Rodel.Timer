using Rodel.Timer.Core.History;

namespace Rodel.Timer.Core.Timing;

/// <summary>
/// Tracks one run of a timer from <see cref="Start"/> to <see cref="Stop"/>. <see cref="Elapsed"/> is
/// computed from <see cref="TimeProvider.GetTimestamp"/>/<see cref="TimeProvider.GetElapsedTime(long, long)"/>
/// — a monotonic, high-resolution clock — rather than from wall-clock time, so the reported duration is
/// immune to system clock adjustments (NTP sync, daylight saving, or a manual clock change) while the
/// session is running. <see cref="StartedAt"/> is a separate wall-clock timestamp kept only for display; it
/// is never used to compute <see cref="Elapsed"/> or the duration recorded by <see cref="Stop"/>.
/// </summary>
public sealed class TimerSession
{
    private readonly TimeProvider _timeProvider;
    private long _startedTimestamp;

    /// <summary>Creates a session driven by <paramref name="timeProvider"/>.</summary>
    /// <param name="timeProvider">The time source used for both the monotonic elapsed-time clock and the wall-clock timestamps.</param>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public TimerSession(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>Whether the session is currently running, i.e. between a <see cref="Start"/> and its matching <see cref="Stop"/>.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>The wall-clock time at which the session was started, or <see langword="null"/> if it is not running.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// The time elapsed since <see cref="Start"/>, measured with the monotonic timestamp clock; <see cref="TimeSpan.Zero"/> if not running.
    /// </summary>
    public TimeSpan Elapsed => IsRunning
        ? _timeProvider.GetElapsedTime(_startedTimestamp, _timeProvider.GetTimestamp())
        : TimeSpan.Zero;

    /// <summary>Starts the session, capturing both the wall-clock <see cref="StartedAt"/> and the monotonic start timestamp.</summary>
    /// <exception cref="InvalidOperationException">The session is already running.</exception>
    public void Start()
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("The timer is already running.");
        }

        StartedAt = _timeProvider.GetUtcNow();
        _startedTimestamp = _timeProvider.GetTimestamp();
        IsRunning = true;
    }

    /// <summary>
    /// Stops the session and returns a <see cref="History.TimerEntry"/> capturing its monotonic elapsed
    /// duration (floored to whole seconds) and the current wall-clock time as its creation timestamp.
    /// </summary>
    /// <param name="entryId">The identifier for the resulting entry. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="folderId">The folder the resulting entry belongs to. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="title">The title for the resulting entry. Must not be null, empty, or whitespace-only.</param>
    /// <returns>A new <see cref="History.TimerEntry"/> describing the just-finished session.</returns>
    /// <exception cref="InvalidOperationException">The session is not running.</exception>
    public TimerEntry Stop(Guid entryId, Guid folderId, string title)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("The timer is not running.");
        }

        TimeSpan elapsed = _timeProvider.GetElapsedTime(_startedTimestamp, _timeProvider.GetTimestamp());
        DateTimeOffset createdAt = _timeProvider.GetUtcNow();
        var entry = new TimerEntry(
            entryId,
            folderId,
            title,
            (long)Math.Floor(elapsed.TotalSeconds),
            createdAt);

        IsRunning = false;
        StartedAt = null;
        _startedTimestamp = 0;
        return entry;
    }
}
