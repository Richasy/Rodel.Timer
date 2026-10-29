using Rodel.Timer.Tests.TestSupport;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAppStoreCloseTests
{
    [TestMethod]
    public async Task Close_FlushesThePendingSave()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);
        Assert.AreEqual(0, harness.SaveCount);

        Task close = harness.Store.CloseAsync();
        Assert.AreEqual(1, harness.SaveCount, "Close must flush the pending debounced save immediately.");

        harness.Scheduler.DrainUntilIdle();
        await close;

        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
    }

    [TestMethod]
    public async Task Close_WithNoPendingButAPriorError_ForcesARetry()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        harness.NextSaveError = new IOException("first attempt failed");
        harness.Store.UpdateDailyTarget(90);
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        harness.Scheduler.DrainUntilIdle();
        Assert.IsTrue(harness.Store.SaveState.IsError);
        Assert.AreEqual(1, harness.SaveCount);

        // Nothing is pending now, but the prior failure forces Close to retry.
        Task close = harness.Store.CloseAsync();
        Assert.AreEqual(2, harness.SaveCount);

        harness.Scheduler.DrainUntilIdle();
        await close;
        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
    }

    [TestMethod]
    public async Task Close_Failure_ReopensTheStore_AndRecloseRetries()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        harness.NextSaveError = new IOException("close save failed");
        harness.Store.UpdateDailyTarget(90);

        Task close = harness.Store.CloseAsync();
        harness.Scheduler.DrainUntilIdle();
        await Assert.ThrowsAsync<IOException>(() => close);

        // The failed close re-opened the store, so mutations resume.
        harness.Store.UpdateDailyTarget(91);

        Task reclose = harness.Store.CloseAsync();
        harness.Scheduler.DrainUntilIdle();
        await reclose;
        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
    }

    [TestMethod]
    public async Task Close_Failure_ReopensOnlyAfterTheHomeSchedulerCommitsIt()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.NextSaveError = new IOException("close save failed");
        harness.Store.UpdateDailyTarget(90);

        Task close = harness.Store.CloseAsync();
        harness.Scheduler.DrainOnce();

        Assert.IsTrue(harness.Store.SaveState.IsError, "The save result has reached the home scheduler.");
        Assert.IsFalse(close.IsCompleted, "Close must marshal its own state transition through the home scheduler.");
        Assert.Throws<InvalidOperationException>(() => harness.Store.UpdateDailyTarget(91));

        harness.Scheduler.DrainUntilIdle();
        await Assert.ThrowsAsync<IOException>(() => close);
        harness.Store.UpdateDailyTarget(91);
        Assert.AreEqual(91, harness.Store.Preferences.DailyTargetMinutes);
    }

    [TestMethod]
    public void Close_ConcurrentCalls_ObserveTheSameTask()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);

        Task first = harness.Store.CloseAsync();
        Task second = harness.Store.CloseAsync();

        Assert.AreSame(first, second);

        harness.Scheduler.DrainUntilIdle();
    }

    [TestMethod]
    public async Task Close_Success_BlocksFurtherMutations()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);

        Task close = harness.Store.CloseAsync();
        harness.Scheduler.DrainUntilIdle();
        await close;

        Assert.Throws<InvalidOperationException>(() => harness.Store.UpdateDailyTarget(91));
    }

    [TestMethod]
    public async Task FlushSaveAsync_FlushesWithoutClosingTheStore()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);

        Task flush = harness.Store.FlushSaveAsync();
        Assert.AreEqual(1, harness.SaveCount);

        harness.Scheduler.DrainUntilIdle();
        await flush;
        Assert.IsTrue(harness.Store.SaveState.IsSuccess);

        // Not closing: mutations still work.
        harness.Store.UpdateDailyTarget(91);
        Assert.AreEqual(91, harness.Store.Preferences.DailyTargetMinutes);
    }
}
