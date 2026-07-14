using Rodel.Timer.Core.History;
using Rodel.Timer.Tests.TestSupport;

using Sprout.Reactive;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAppStoreTimerTests
{
    [TestMethod]
    public void StartTimer_SetsRunning_WithoutMutatingOrSaving()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        int revision = harness.Store.Revision;

        harness.Store.StartTimer();

        Assert.IsTrue(harness.Store.IsRunning);
        Assert.AreEqual(revision, harness.Store.Revision);
        Assert.AreEqual(0, harness.SaveCount);
        Assert.AreEqual(0, harness.Scheduler.PendingCallbackCount);
    }

    [TestMethod]
    public void StartTimer_WhenAlreadyRunning_Throws()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();

        Assert.Throws<InvalidOperationException>(harness.Store.StartTimer);
    }

    [TestMethod]
    public void Elapsed_ReadsTheMonotonicClock()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();

        harness.Time.Advance(TimeSpan.FromSeconds(42));

        Assert.AreEqual(TimeSpan.FromSeconds(42), harness.Store.Elapsed);
    }

    [TestMethod]
    public void Elapsed_IsImmuneToAWallClockShift()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromSeconds(10));

        harness.Time.ShiftWallClock(TimeSpan.FromHours(-3));

        Assert.AreEqual(TimeSpan.FromSeconds(10), harness.Store.Elapsed);
    }

    [TestMethod]
    public void Elapsed_IsZeroWhenIdle()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.AreEqual(TimeSpan.Zero, harness.Store.Elapsed);
        Assert.IsNull(harness.Store.RunningStartedAt);
    }

    [TestMethod]
    public void RunningStartedAt_ReportsTheWallClockStart()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        DateTimeOffset start = harness.Time.GetUtcNow();

        harness.Store.StartTimer();

        Assert.AreEqual(start, harness.Store.RunningStartedAt);
    }

    [TestMethod]
    public void Tick_OnlyBumpsWhileRunning()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        int idleTick = harness.Store.DisplayTick;

        harness.Store.Tick();
        Assert.AreEqual(idleTick, harness.Store.DisplayTick);

        harness.Store.StartTimer();
        harness.Store.Tick();
        Assert.AreEqual(idleTick + 1, harness.Store.DisplayTick);
    }

    [TestMethod]
    public void Tick_DrivesElapsedReactivity()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();

        int runs = 0;
        using var effect = new Effect(() =>
        {
            _ = harness.Store.Elapsed;
            runs++;
        });

        Assert.AreEqual(1, runs);

        harness.Time.Advance(TimeSpan.FromSeconds(1));
        harness.Store.Tick();
        Assert.AreEqual(2, runs);
    }

    [TestMethod]
    public void StopTimer_RecordsEntryUnderTheFolderSelectedAtStop()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromSeconds(90));

        // Switch the selected folder mid-session: the entry must land in the folder current at the stop moment.
        harness.Store.SelectFolder(TimerStoreHarness.FolderBId);
        TimerEntry entry = harness.Store.StopTimer("Focus block");

        Assert.AreEqual(TimerStoreHarness.FolderBId, entry.FolderId);
        Assert.AreEqual(90, entry.DurationSeconds);
        Assert.AreEqual("Focus block", entry.Title);
        Assert.IsFalse(harness.Store.IsRunning);
        Assert.AreEqual(TimeSpan.Zero, harness.Store.Elapsed);
        Assert.IsTrue(harness.Store.Entries.Any(item => item.Id == entry.Id));
    }

    [TestMethod]
    public void StopTimer_FloorsElapsedToWholeSeconds()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromMilliseconds(90_750));

        TimerEntry entry = harness.Store.StopTimer("Partial");

        Assert.AreEqual(90, entry.DurationSeconds);
    }

    [TestMethod]
    public void StopTimer_EmptyTitle_ThrowsAndKeepsRunning()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromSeconds(5));

        Assert.Throws<ArgumentException>(() => harness.Store.StopTimer("   "));

        Assert.IsTrue(harness.Store.IsRunning);
        Assert.AreEqual(TimeSpan.FromSeconds(5), harness.Store.Elapsed);
    }

    [TestMethod]
    public void StopTimer_IdCollision_ThrowsKeepsRunning_ThenSucceedsWithAFreshId()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        // Record a first entry so its id is already present in history.
        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromSeconds(10));
        TimerEntry firstEntry = harness.Store.StopTimer("First");

        // Time a second, 42-second session, then force the id factory to hand back the existing entry's id.
        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromSeconds(42));
        harness.IdFactory = () => firstEntry.Id;
        int entriesBefore = harness.Store.Entries.Count;
        int revisionBefore = harness.Store.Revision;

        Assert.Throws<InvalidOperationException>(() => harness.Store.StopTimer("Focus"));

        // The colliding stop must leave the session running with its full elapsed intact and record nothing.
        Assert.IsTrue(harness.Store.IsRunning);
        Assert.AreEqual(TimeSpan.FromSeconds(42), harness.Store.Elapsed);
        Assert.HasCount(entriesBefore, harness.Store.Entries);
        Assert.AreEqual(revisionBefore, harness.Store.Revision);

        // Retrying with a usable id records the complete 42-second session.
        Guid freshId = Guid.NewGuid();
        harness.IdFactory = () => freshId;
        TimerEntry recorded = harness.Store.StopTimer("Focus");

        Assert.AreEqual(freshId, recorded.Id);
        Assert.AreEqual(42, recorded.DurationSeconds);
        Assert.IsFalse(harness.Store.IsRunning);
        Assert.IsTrue(harness.Store.Entries.Any(item => item.Id == freshId));
    }

    [TestMethod]
    public void StopTimer_WhenNotRunning_Throws()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.Throws<InvalidOperationException>(() => harness.Store.StopTimer("x"));
    }

    [TestMethod]
    public void StopTimer_BumpsRevisionAndArmsSave()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromSeconds(30));
        int revision = harness.Store.Revision;

        harness.Store.StopTimer("Session");

        Assert.AreEqual(revision + 1, harness.Store.Revision);
        Assert.IsGreaterThan(0, harness.Scheduler.PendingCallbackCount);
    }

    [TestMethod]
    public void TodayTotal_CountsCompletedHistory_ExcludingTheRunningSession()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.AreEqual(TimeSpan.Zero, harness.Store.TodayTotal);

        harness.Store.StartTimer();
        harness.Time.Advance(TimeSpan.FromSeconds(60));
        Assert.AreEqual(TimeSpan.Zero, harness.Store.TodayTotal);

        harness.Store.StopTimer("Done");
        Assert.AreEqual(TimeSpan.FromSeconds(60), harness.Store.TodayTotal);
    }

    [TestMethod]
    public void Elapsed_IsMonotonicAcrossSuccessiveReads()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.StartTimer();

        harness.Time.Advance(TimeSpan.FromSeconds(1));
        TimeSpan first = harness.Store.Elapsed;
        harness.Time.Advance(TimeSpan.FromSeconds(1));
        TimeSpan second = harness.Store.Elapsed;

        Assert.IsTrue(second >= first);
        Assert.AreEqual(TimeSpan.FromSeconds(2), second);
    }
}
