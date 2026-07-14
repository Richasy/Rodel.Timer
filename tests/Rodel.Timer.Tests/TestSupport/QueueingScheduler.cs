using Sprout.Reactive;

namespace Rodel.Timer.Tests.TestSupport;

/// <summary>
/// A deterministic <see cref="IScheduler"/> for headless store tests. Unlike Sprout's inline <c>ManualScheduler</c>,
/// this one <b>queues</b> <see cref="Post"/> callbacks instead of running them inline, so a test can prove that an
/// async operation's state commit (Loading / Success / Error) only lands when the queue is explicitly drained —
/// exactly the "commit only on drain" guarantee the store relies on. Delayed <see cref="Schedule"/> callbacks (the
/// debounce timer) are driven separately by <see cref="Advance"/>, so debounce timing and async commits are advanced
/// independently with zero wall-clock race.
/// </summary>
public sealed class QueueingScheduler : IScheduler
{
    private readonly List<Scheduled> _scheduled = [];
    private readonly Queue<Action> _posts = new();

    /// <summary>The current instant. Settable so a test can pin an origin; <see cref="Advance"/> moves it forward.</summary>
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;

    /// <summary>The number of still-pending scheduled (delayed) callbacks — for asserting a debounce timer is armed or cancelled.</summary>
    public int PendingCallbackCount => _scheduled.Count(item => !item.Cancelled);

    /// <summary>The number of queued <see cref="Post"/> callbacks not yet drained.</summary>
    public int PendingPostCount => _posts.Count;

    /// <inheritdoc/>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _posts.Enqueue(action);
    }

    /// <inheritdoc/>
    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var item = new Scheduled(Now + delay, callback);
        _scheduled.Add(item);
        return new Handle(item);
    }

    /// <summary>
    /// Runs every callback that was queued by <see cref="Post"/> at the moment of this call (a single tick). Callbacks
    /// queued <em>during</em> the drain are left for the next tick, so a test can step one commit at a time.
    /// </summary>
    /// <returns><see langword="true"/> if at least one callback ran; otherwise <see langword="false"/>.</returns>
    public bool DrainOnce()
    {
        int batch = _posts.Count;
        if (batch == 0)
        {
            return false;
        }

        for (int index = 0; index < batch; index++)
        {
            _posts.Dequeue().Invoke();
        }

        return true;
    }

    /// <summary>Repeatedly drains queued <see cref="Post"/> callbacks until the queue stays empty.</summary>
    public void DrainUntilIdle()
    {
        while (DrainOnce())
        {
        }
    }

    /// <summary>
    /// Advances the clock by <paramref name="by"/> and fires every delayed callback now due, in due order, moving the
    /// clock to each callback's due instant before it runs and re-scanning afterward so a callback that re-arms a
    /// nearer timer (the debounce trailing/maxWait cascade) is honoured. Does not drain <see cref="Post"/> callbacks.
    /// </summary>
    /// <param name="by">The amount to advance the clock by.</param>
    public void Advance(TimeSpan by)
    {
        DateTimeOffset target = Now + by;
        while (true)
        {
            Scheduled? next = _scheduled
                .Where(item => !item.Cancelled && item.DueAt <= target)
                .OrderBy(item => item.DueAt)
                .FirstOrDefault();
            if (next is null)
            {
                break;
            }

            Now = next.DueAt;
            next.Cancelled = true;
            _scheduled.Remove(next);
            next.Callback();
        }

        Now = target;
    }

    private sealed class Scheduled(DateTimeOffset dueAt, Action callback)
    {
        public DateTimeOffset DueAt { get; } = dueAt;

        public Action Callback { get; } = callback;

        public bool Cancelled { get; set; }
    }

    private sealed class Handle(Scheduled item) : IDisposable
    {
        public void Dispose() => item.Cancelled = true;
    }
}
