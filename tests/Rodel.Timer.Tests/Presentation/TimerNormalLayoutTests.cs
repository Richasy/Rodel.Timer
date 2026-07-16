using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerNormalLayoutTests
{
    [TestMethod]
    public void WideTall_UsesFullHierarchy()
    {
        TimerNormalLayout layout = TimerNormalLayoutResolver.Resolve(1100d, 720d);

        Assert.IsTrue(layout.ShowMiddleStatus);
        Assert.AreEqual(96f, layout.ClockSize);
        Assert.AreEqual(280f, layout.AxisSegmentWidth);
        Assert.AreEqual(36f, layout.StageVerticalPadding);
        Assert.AreEqual(14f, layout.FooterVerticalPadding);
    }

    [TestMethod]
    public void NarrowShort_RemovesMiddleStatusAndCompressesVerticalMetrics()
    {
        TimerNormalLayout layout = TimerNormalLayoutResolver.Resolve(640d, 480d);

        Assert.IsFalse(layout.ShowMiddleStatus);
        Assert.AreEqual(58f, layout.ClockSize);
        Assert.AreEqual(120f, layout.AxisSegmentWidth);
        Assert.AreEqual(18f, layout.StageVerticalPadding);
        Assert.AreEqual(10f, layout.FooterVerticalPadding);
    }

    [TestMethod]
    public void Breakpoints_AreInclusiveForFullStates()
    {
        TimerNormalLayout layout = TimerNormalLayoutResolver.Resolve(
            TimerNormalLayoutResolver.NarrowThreshold,
            TimerNormalLayoutResolver.ShortThreshold);

        Assert.IsTrue(layout.ShowMiddleStatus);
        Assert.AreEqual(96f, layout.ClockSize);
    }

    [TestMethod]
    public void AxisSegments_ClampAtBothEnds()
    {
        Assert.AreEqual(48f, TimerNormalLayoutResolver.Resolve(300d, 720d).AxisSegmentWidth);
        Assert.AreEqual(280f, TimerNormalLayoutResolver.Resolve(2000d, 720d).AxisSegmentWidth);
    }
}
