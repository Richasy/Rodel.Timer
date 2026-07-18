using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerTitleBarLayoutTests
{
    [TestMethod]
    public void NormalWide_ShowsBrandAndCommandLabels()
    {
        TimerTitleBarLayout layout = TimerTitleBarLayoutResolver.Resolve(1100d, isCompact: false);

        Assert.IsTrue(layout.IsVisible);
        Assert.IsTrue(layout.ShowBrandLabel);
        Assert.IsTrue(layout.ShowCommandLabels);
    }

    [TestMethod]
    public void NormalNarrow_KeepsCommandsButCollapsesTheirLabels()
    {
        TimerTitleBarLayout layout = TimerTitleBarLayoutResolver.Resolve(640d, isCompact: false);

        Assert.IsTrue(layout.IsVisible);
        Assert.IsFalse(layout.ShowBrandLabel);
        Assert.IsFalse(layout.ShowCommandLabels);
    }

    [TestMethod]
    public void LabelThreshold_IsInclusive()
    {
        Assert.IsFalse(
            TimerTitleBarLayoutResolver.Resolve(
                TimerTitleBarLayoutResolver.LabelThreshold - 0.01d,
                isCompact: false).ShowCommandLabels);
        Assert.IsTrue(
            TimerTitleBarLayoutResolver.Resolve(
                TimerTitleBarLayoutResolver.LabelThreshold,
                isCompact: false).ShowCommandLabels);
    }

    [TestMethod]
    public void Compact_RemovesTheWholeExtendedTitleBar()
    {
        TimerTitleBarLayout layout = TimerTitleBarLayoutResolver.Resolve(1100d, isCompact: true);

        Assert.IsFalse(layout.IsVisible);
        Assert.IsFalse(layout.ShowBrandLabel);
        Assert.IsFalse(layout.ShowCommandLabels);
    }
}
