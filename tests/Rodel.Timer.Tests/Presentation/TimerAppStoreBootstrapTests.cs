using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Workspace;
using Rodel.Timer.Tests.TestSupport;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAppStoreBootstrapTests
{
    [TestMethod]
    public void Bootstrap_StartsIdle_WithNoWorkspace()
    {
        using var harness = new TimerStoreHarness();

        Assert.IsTrue(harness.Store.Bootstrap.IsIdle);
        Assert.IsFalse(harness.Store.Bootstrap.HasValue);
    }

    [TestMethod]
    public void WorkspaceGetters_BeforeLoad_Throw()
    {
        using var harness = new TimerStoreHarness();

        Assert.Throws<InvalidOperationException>(() => _ = harness.Store.Folders);
        Assert.Throws<InvalidOperationException>(() => _ = harness.Store.Entries);
        Assert.Throws<InvalidOperationException>(() => _ = harness.Store.SelectedFolder);
        Assert.Throws<InvalidOperationException>(() => _ = harness.Store.Preferences);
        Assert.Throws<InvalidOperationException>(() => _ = harness.Store.TodayTotal);
    }

    [TestMethod]
    public void Load_MovesIdleToLoadingToSuccess_CommittingOnlyOnDrain()
    {
        using var harness = new TimerStoreHarness();

        Task load = harness.BeginLoad();

        // The Loading commit is posted, not applied: the state is still Idle until the queue is drained.
        Assert.IsTrue(harness.Store.Bootstrap.IsIdle);
        Assert.AreEqual(1, harness.Scheduler.PendingPostCount);

        Assert.IsTrue(harness.Scheduler.DrainOnce());
        Assert.IsTrue(harness.Store.Bootstrap.IsLoading);

        harness.CompleteLoad();

        // The terminal commit is likewise deferred to a drain.
        Assert.IsTrue(harness.Store.Bootstrap.IsLoading);

        harness.Scheduler.DrainUntilIdle();
        Assert.IsTrue(harness.Store.Bootstrap.IsSuccess);
        Assert.IsTrue(harness.Store.Bootstrap.HasValue);
        Assert.IsTrue(load.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void Load_Success_MakesWorkspaceReadableWithNoWindow()
    {
        using var harness = new TimerStoreHarness();

        harness.LoadToReady();

        Assert.HasCount(2, harness.Store.Folders);
        Assert.AreEqual(TimerStoreHarness.FolderAId, harness.Store.SelectedFolder.Id);
        Assert.IsEmpty(harness.Store.Entries);
    }

    [TestMethod]
    public void Load_WorkError_SurfacesInBootstrap_NotAsRunFault()
    {
        using var harness = new TimerStoreHarness();

        Task load = harness.BeginLoad();
        harness.Scheduler.DrainOnce();

        var failure = new InvalidOperationException("bootstrap failed");
        harness.FailLoad(failure);
        harness.Scheduler.DrainUntilIdle();

        Assert.IsTrue(harness.Store.Bootstrap.IsError);
        Assert.AreSame(failure, harness.Store.Bootstrap.Error);
        Assert.IsTrue(load.IsCompletedSuccessfully, "A work error must be captured into state, not fault the Run task.");
    }

    [TestMethod]
    public void Load_AfterSuccess_IsRejected_KeepingTheAggregate()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        TimerWorkspace loaded = harness.Store.Bootstrap.Value!.Workspace;

        // Load-once: a call after the workspace is available must not reload (that would replace the aggregate).
        Assert.Throws<InvalidOperationException>(() => harness.Store.LoadAsync());

        Assert.AreSame(loaded, harness.Store.Bootstrap.Value!.Workspace);
        Assert.AreEqual(1, harness.LoadCount, "A rejected reload must not invoke the load delegate.");
    }

    [TestMethod]
    public void Load_AfterPendingMutation_IsRejected_AndNeitherTheEditNorItsSaveIsLost()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        TimerWorkspace edited = harness.Store.Bootstrap.Value!.Workspace;

        // A pending, not-yet-persisted edit arms the debounced auto-save.
        harness.Store.UpdateDailyTarget(123);
        Assert.IsGreaterThan(0, harness.Scheduler.PendingCallbackCount);

        // Reloading here would swap the aggregate out from under the armed save, which would then persist the freshly
        // loaded workspace over the edit. The load-once contract refuses it.
        Assert.Throws<InvalidOperationException>(() => harness.Store.LoadAsync());
        Assert.AreEqual(1, harness.LoadCount);
        Assert.AreSame(edited, harness.Store.Bootstrap.Value!.Workspace);

        // The armed auto-save fires and persists the *edited* aggregate — the edit is not silently dropped.
        harness.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
        harness.Scheduler.DrainUntilIdle();

        Assert.HasCount(1, harness.SavedWorkspaces);
        Assert.AreSame(edited, harness.SavedWorkspaces[0]);
        Assert.AreEqual(123, harness.SavedWorkspaces[0].Preferences.DailyTargetMinutes);
        Assert.IsTrue(harness.Store.SaveState.IsSuccess);
    }

    [TestMethod]
    public void Load_InitialConcurrentCalls_AreLatestWins()
    {
        using var harness = new TimerStoreHarness();

        Task first = harness.BeginLoad();
        harness.Scheduler.DrainOnce();
        Assert.IsTrue(harness.Store.Bootstrap.IsLoading);

        // No value yet, so a second initial load is permitted and supersedes (cancels) the first.
        Task second = harness.BeginLoad();
        var winning = harness.BuildWorkspace();
        harness.CompleteLoad(new TimerBootstrapResult(winning, TimerBootstrapOrigin.Existing, []));
        harness.Scheduler.DrainUntilIdle();

        Assert.IsTrue(harness.Store.Bootstrap.IsSuccess);
        Assert.AreSame(winning, harness.Store.Bootstrap.Value!.Workspace);
        Assert.IsTrue(first.IsCompleted);
        Assert.IsTrue(second.IsCompletedSuccessfully);
        Assert.AreEqual(2, harness.LoadCount);
    }

    [TestMethod]
    public void Load_AfterValuelessError_CanRetry()
    {
        using var harness = new TimerStoreHarness();

        harness.BeginLoad();
        harness.Scheduler.DrainOnce();
        harness.FailLoad(new InvalidOperationException("initial load failed"));
        harness.Scheduler.DrainUntilIdle();

        Assert.IsTrue(harness.Store.Bootstrap.IsError);
        Assert.IsFalse(harness.Store.Bootstrap.HasValue);

        // A value-less error is retryable (HasValue is still false).
        harness.LoadToReady();

        Assert.IsTrue(harness.Store.Bootstrap.IsSuccess);
        Assert.AreEqual(2, harness.LoadCount);
    }

    [TestMethod]
    public void Load_Cancellation_ViaToken_LeavesItRetryable()
    {
        using var harness = new TimerStoreHarness();

        using var cts = new CancellationTokenSource();
        Task load = harness.Store.LoadAsync(cts.Token);
        harness.Scheduler.DrainOnce();
        Assert.IsTrue(harness.Store.Bootstrap.IsLoading);

        cts.Cancel();
        harness.Scheduler.DrainUntilIdle();

        // The cancelled attempt committed no value, so a fresh load is still permitted and succeeds.
        Assert.IsFalse(harness.Store.Bootstrap.HasValue);
        Assert.IsTrue(load.IsCompleted);

        harness.LoadToReady();
        Assert.IsTrue(harness.Store.Bootstrap.IsSuccess);
    }
}
