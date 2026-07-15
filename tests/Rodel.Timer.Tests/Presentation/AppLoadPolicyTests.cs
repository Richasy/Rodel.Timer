using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of <see cref="AppLoadPolicy"/> — the guard that decides whether a completed data-load may apply its result.
/// A load kicked off at startup can complete after the window is closed (the store disposed), and it also completes
/// normally when the bootstrap failed internally, so the continuation must only apply the loaded state when the store is
/// alive <b>and</b> the bootstrap produced a value.
/// </summary>
[TestClass]
public sealed class AppLoadPolicyTests
{
    [TestMethod]
    public void AliveAndSucceeded_Applies() =>
        Assert.IsTrue(AppLoadPolicy.ShouldApplyLoadedData(storeDisposed: false, bootstrapSucceeded: true));

    [TestMethod]
    public void DisposedDuringLoad_DoesNotApply() =>
        Assert.IsFalse(AppLoadPolicy.ShouldApplyLoadedData(storeDisposed: true, bootstrapSucceeded: true));

    [TestMethod]
    public void CompletedButBootstrapFailed_DoesNotApply() =>
        Assert.IsFalse(AppLoadPolicy.ShouldApplyLoadedData(storeDisposed: false, bootstrapSucceeded: false));

    [TestMethod]
    public void DisposedAndFailed_DoesNotApply() =>
        Assert.IsFalse(AppLoadPolicy.ShouldApplyLoadedData(storeDisposed: true, bootstrapSucceeded: false));
}
