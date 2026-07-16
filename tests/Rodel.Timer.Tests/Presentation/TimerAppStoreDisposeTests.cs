using Rodel.Timer.Core.Settings;
using Rodel.Timer.Tests.TestSupport;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAppStoreDisposeTests
{
    [TestMethod]
    public void Dispose_CancelsThePendingDebounceTimer()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateDailyTarget(90);
        Assert.IsGreaterThan(0, harness.Scheduler.PendingCallbackCount);

        harness.Store.Dispose();

        Assert.AreEqual(0, harness.Scheduler.PendingCallbackCount);
    }

    [TestMethod]
    public void Dispose_MakesEveryPublicMethodThrowObjectDisposed()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.Dispose();

        Assert.Throws<ObjectDisposedException>(() => harness.Store.LoadAsync());
        Assert.Throws<ObjectDisposedException>(harness.Store.Tick);
        Assert.Throws<ObjectDisposedException>(() => harness.Store.SelectFolder(TimerStoreHarness.FolderAId));
        Assert.Throws<ObjectDisposedException>(() => harness.Store.UpdateBackground(TimerBackgroundMode.Solid));
        Assert.Throws<ObjectDisposedException>(() => harness.Store.UpdateDailyTarget(90));
        Assert.Throws<ObjectDisposedException>(harness.Store.StartTimer);
        Assert.Throws<ObjectDisposedException>(() => harness.Store.CloseAsync());
        Assert.Throws<ObjectDisposedException>(() => harness.Store.FlushSaveAsync());
    }

    [TestMethod]
    public void Dispose_IsIdempotent()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        harness.Store.Dispose();
        harness.Store.Dispose();
    }

    [TestMethod]
    public void Dispose_DropsALateBootstrapCommit()
    {
        using var harness = new TimerStoreHarness();
        harness.BeginLoad();
        harness.Scheduler.DrainOnce();

        // The load resolves and posts its terminal (success) commit, but it is still queued (not drained).
        harness.CompleteLoad();
        Assert.IsGreaterThan(0, harness.Scheduler.PendingPostCount);

        // Disposing cancels the lifetime before the queued commit runs; the commit guard must drop it.
        harness.Store.Dispose();
        harness.Scheduler.DrainUntilIdle();

        Assert.IsFalse(harness.Store.Bootstrap.IsSuccess);
    }
}
