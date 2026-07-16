using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAnchoredSurfaceLayoutTests
{
    [TestMethod]
    public void OneFolder_ShrinksToItsContent()
    {
        TimerAnchoredSurfaceLayout layout = TimerAnchoredSurfaceLayoutResolver.Resolve(1, 72f);

        Assert.AreEqual(336f, layout.Width);
        Assert.AreEqual(188f, layout.Height);
    }

    [TestMethod]
    public void EmptyHistory_ReservesAReadableEmptyState()
    {
        TimerAnchoredSurfaceLayout layout = TimerAnchoredSurfaceLayoutResolver.Resolve(0, 76f);

        Assert.AreEqual(192f, layout.Height);
    }

    [TestMethod]
    public void ManyRows_CapAtScrollingHeight()
    {
        TimerAnchoredSurfaceLayout layout = TimerAnchoredSurfaceLayoutResolver.Resolve(20, 76f);

        Assert.AreEqual(TimerAnchoredSurfaceLayoutResolver.MaximumHeight, layout.Height);
    }

    [TestMethod]
    public void InvalidInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TimerAnchoredSurfaceLayoutResolver.Resolve(-1, 72f));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TimerAnchoredSurfaceLayoutResolver.Resolve(1, 0f));
    }
}
