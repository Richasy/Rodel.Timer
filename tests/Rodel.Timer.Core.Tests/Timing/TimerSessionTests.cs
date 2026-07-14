using Rodel.Timer.Core.Tests.TestSupport;
using Rodel.Timer.Core.Timing;

namespace Rodel.Timer.Core.Tests.Timing;

[TestClass]
public sealed class TimerSessionTests
{
    [TestMethod]
    public void Stop_UsesMonotonicElapsedTime_WhenWallClockMovesBackward()
    {
        var timeProvider = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 14, 1, 0, 0, TimeSpan.Zero));
        var session = new TimerSession(timeProvider);
        Guid folderId = Guid.NewGuid();
        Guid entryId = Guid.NewGuid();

        session.Start();
        timeProvider.Advance(TimeSpan.FromSeconds(90.9));
        timeProvider.ShiftWallClock(TimeSpan.FromDays(-1));

        var entry = session.Stop(entryId, folderId, "Deep work");

        Assert.AreEqual(90L, entry.DurationSeconds);
        Assert.AreEqual(entryId, entry.Id);
        Assert.AreEqual(folderId, entry.FolderId);
        Assert.AreEqual("Deep work", entry.Title);
        Assert.AreEqual(
            new DateTimeOffset(2026, 7, 13, 1, 1, 30, 900, TimeSpan.Zero),
            entry.CreatedAt);
        Assert.IsFalse(session.IsRunning);
        Assert.IsNull(session.StartedAt);
        Assert.AreEqual(TimeSpan.Zero, session.Elapsed);
    }

    [TestMethod]
    public void Start_RejectsASecondConcurrentTimer()
    {
        var session = new TimerSession(
            new ManualTimeProvider(DateTimeOffset.UnixEpoch));
        session.Start();

        Assert.Throws<InvalidOperationException>(session.Start);
        Assert.IsTrue(session.IsRunning);
    }

    [TestMethod]
    public void Stop_RejectsWhenTimerIsNotRunning()
    {
        var session = new TimerSession(
            new ManualTimeProvider(DateTimeOffset.UnixEpoch));

        Assert.Throws<InvalidOperationException>(
            () => session.Stop(Guid.NewGuid(), Guid.NewGuid(), "Task"));
    }

    [TestMethod]
    public void Stop_PreservesRunningSessionWhenEntryValidationFails()
    {
        var session = new TimerSession(
            new ManualTimeProvider(DateTimeOffset.UnixEpoch));
        session.Start();

        Assert.Throws<ArgumentException>(
            () => session.Stop(Guid.NewGuid(), Guid.NewGuid(), " "));

        Assert.IsTrue(session.IsRunning);
        Assert.IsNotNull(session.StartedAt);
    }
}
