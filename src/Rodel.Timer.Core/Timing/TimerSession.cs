using Rodel.Timer.Core.History;

namespace Rodel.Timer.Core.Timing;

public sealed class TimerSession
{
    private readonly TimeProvider _timeProvider;
    private long _startedTimestamp;

    public TimerSession(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public bool IsRunning { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public TimeSpan Elapsed => IsRunning
        ? _timeProvider.GetElapsedTime(_startedTimestamp, _timeProvider.GetTimestamp())
        : TimeSpan.Zero;

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
