using Rodel.Timer.Tests.TestSupport;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAppStoreSaveTests
{
    [TestMethod]
    public void Save_TrailingEdge_FiresOnlyAfterTheQuietPeriod()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);

        Assert.AreEqual(0, harness.SaveCount);

        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(499));
        Assert.AreEqual(0, harness.SaveCount);

        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(1));
        Assert.AreEqual(1, harness.SaveCount);
    }

    [TestMethod]
    public void Save_CommitsSaveStateOnlyOnDrain()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));

        // The save work has run, but its Success commit is queued until the scheduler drains.
        Assert.AreEqual(1, harness.SaveCount);
        Assert.IsTrue(harness.Store.SaveState.IsIdle || harness.Store.SaveState.IsLoading);

        harness.Scheduler.DrainUntilIdle();
        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
    }

    [TestMethod]
    public void Save_Coalesces_ABurstIntoASingleWrite()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        harness.Store.UpdateDailyTarget(90);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(100));
        harness.Store.UpdateDailyTarget(91);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(100));
        harness.Store.UpdateDailyTarget(92);

        Assert.AreEqual(0, harness.SaveCount);

        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        Assert.AreEqual(1, harness.SaveCount);
    }

    [TestMethod]
    public void Save_MaxWait_ForcesAWriteDuringContinuousActivity()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        // Keep mutating faster than the 500 ms quiet period; the 2 s maxWait must still force a save.
        for (int index = 0; index < 7; index++)
        {
            harness.Store.UpdateDailyTarget(60 + index);
            harness.Scheduler.Advance(TimeSpan.FromMilliseconds(300));
        }

        Assert.IsGreaterThanOrEqualTo(1, harness.SaveCount);
    }

    [TestMethod]
    public void Save_SequentialOperation_DoesNotOverlap()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        var gate = new TaskCompletionSource();
        harness.SaveGate = gate;

        harness.Store.UpdateDailyTarget(90);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        Assert.AreEqual(1, harness.SaveCount);

        // A second save is requested while the first is still in flight (blocked on the gate).
        harness.SaveGate = null;
        harness.Store.UpdateDailyTarget(91);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        Assert.AreEqual(1, harness.SaveCount, "The second save must wait for the first to settle.");

        gate.SetResult();
        harness.Scheduler.DrainUntilIdle();
        Assert.AreEqual(2, harness.SaveCount);
        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
    }

    [TestMethod]
    public void Save_Failure_IsObservable_AndASubsequentSaveRecovers()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        harness.NextSaveError = new InvalidOperationException("disk full");
        harness.Store.UpdateDailyTarget(50);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        harness.Scheduler.DrainUntilIdle();

        Assert.IsTrue(harness.Store.SaveState.IsError);
        Assert.AreEqual("disk full", harness.Store.SaveState.Error!.Message);

        harness.Store.UpdateDailyTarget(51);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        harness.Scheduler.DrainUntilIdle();

        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
        Assert.AreEqual(2, harness.SaveCount);
    }

    [TestMethod]
    public void Save_PersistsTheWorkspaceAsSeenAtExecutionTime()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        harness.Scheduler.DrainUntilIdle();

        Assert.HasCount(1, harness.SavedWorkspaces);
        Assert.AreSame(harness.Store.Bootstrap.Value!.Workspace, harness.SavedWorkspaces[0]);
    }

    [TestMethod]
    public async Task FlushSave_AfterASettledFailure_PerformsAFreshSave()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.NextSaveError = new IOException("disk unavailable");
        harness.Store.UpdateDailyTarget(90);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        harness.Scheduler.DrainUntilIdle();
        Assert.IsTrue(harness.Store.SaveState.IsError);

        Task retry = harness.Store.FlushSaveAsync();

        Assert.AreEqual(2, harness.SaveCount, "Retry must run a new write even after the debounce has settled.");
        harness.Scheduler.DrainUntilIdle();
        await retry;
        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
        Assert.AreEqual(90, harness.SavedWorkspaces[^1].Preferences.DailyTargetMinutes);
    }

    [TestMethod]
    public async Task FlushSave_AfterARepeatedFailure_KeepsTheErrorObservable()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.NextSaveError = new IOException("first failure");
        harness.Store.UpdateDailyTarget(90);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        harness.Scheduler.DrainUntilIdle();
        harness.NextSaveError = new IOException("still unavailable");

        Task retry = harness.Store.FlushSaveAsync();
        harness.Scheduler.DrainUntilIdle();
        await retry;

        Assert.AreEqual(2, harness.SaveCount);
        Assert.AreEqual("still unavailable", harness.Store.SaveState.Error?.Message);
    }
}
