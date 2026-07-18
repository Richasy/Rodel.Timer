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
        Assert.IsTrue(layout.ShowAxisDecoration);
        Assert.AreEqual(110f, layout.ClockSize, "10vw of 1100 is 110, inside the [84, 116] clamp.");
        Assert.AreEqual(880f, layout.AxisTotalWidth, "the axis line caps at 880 (86% of 1100 = 946 > 880).");
        Assert.AreEqual(36f, layout.StageVerticalPadding);
        Assert.AreEqual(14f, layout.FooterVerticalPadding);
    }

    [TestMethod]
    public void NarrowShort_RemovesTheMiddleStatusAndAxis_AndCompressesVerticalMetrics()
    {
        TimerNormalLayout layout = TimerNormalLayoutResolver.Resolve(640d, 480d);

        Assert.IsFalse(layout.ShowMiddleStatus);
        Assert.IsFalse(layout.ShowAxisDecoration, "a narrow window hides the axis decoration so the clock stays centred.");
        Assert.AreEqual(58f, layout.ClockSize, "a short window uses the compact clock size.");
        Assert.AreEqual(190f, layout.GlowHeight, "a short window uses the compact glow ellipse height.");
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
        Assert.IsTrue(layout.ShowAxisDecoration);
        Assert.AreEqual(84f, layout.ClockSize, "at 720 width the 10vw (72) clamps up to the 84 floor.");
    }

    [TestMethod]
    public void ClockSize_ScalesContinuouslyAndClampsAtBothEnds()
    {
        Assert.AreEqual(84f, TimerNormalLayoutResolver.Resolve(300d, 720d).ClockSize, "10vw below the floor clamps to 84.");
        Assert.AreEqual(90f, TimerNormalLayoutResolver.Resolve(900d, 720d).ClockSize, "10vw of 900 is 90.");
        Assert.AreEqual(116f, TimerNormalLayoutResolver.Resolve(2000d, 720d).ClockSize, "10vw above the cap clamps to 116.");
    }

    [TestMethod]
    public void AxisAndGlowGeometry_TrackTheClientWidth()
    {
        TimerNormalLayout wide = TimerNormalLayoutResolver.Resolve(1100d, 720d);
        Assert.AreEqual(880f, wide.AxisTotalWidth, "the axis line caps at 880.");
        Assert.AreEqual(700f, wide.GlowWidth, "the glow caps at 700 (78% of 1100 = 858 > 700).");
        Assert.AreEqual(320f, wide.GlowHeight, "a tall window uses the full glow ellipse height.");

        TimerNormalLayout mid = TimerNormalLayoutResolver.Resolve(760d, 720d);
        Assert.AreEqual((float)(760d * 0.86d), mid.AxisTotalWidth, 0.01f, "below ~1023 wide the axis line is 86% of the client.");
        Assert.AreEqual((float)(760d * 0.78d), mid.GlowWidth, 0.01f, "below ~897 wide the glow is 78% of the client.");
    }
}
