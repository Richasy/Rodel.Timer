using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerSidePanelLayoutTests
{
    private const float TitleBar = 48f;

    [TestMethod]
    public void LeftPanel_SitsTwelveEpxInsideTheTitleBarToFooterBand()
    {
        float footer = TimerNormalLayoutResolver.Resolve(1100d, 720d).FooterHeight;

        TimerSidePanelLayout layout = TimerSidePanelLayoutResolver.Resolve(
            1100d,
            720d,
            TitleBar,
            footer,
            TimerSidePanelSide.Left);

        Assert.AreEqual(12f, layout.Left, "the leading edge keeps the 12 epx inset.");
        Assert.AreEqual(TitleBar + 12f, layout.Top, "the panel starts 12 epx below the extended title bar band.");
        Assert.AreEqual(TimerAnchoredSurfaceLayoutResolver.Width, layout.Width, "the panel shares the anchored-surface width.");
        Assert.AreEqual(720f - footer - 12f - (TitleBar + 12f), layout.Height, 0.01f, "the panel fills the band, inset top and bottom.");
    }

    [TestMethod]
    public void RightPanel_MirrorsTheInsetAndTheSlideDirection()
    {
        float footer = TimerNormalLayoutResolver.Resolve(1100d, 720d).FooterHeight;

        TimerSidePanelLayout left = TimerSidePanelLayoutResolver.Resolve(1100d, 720d, TitleBar, footer, TimerSidePanelSide.Left);
        TimerSidePanelLayout right = TimerSidePanelLayoutResolver.Resolve(1100d, 720d, TitleBar, footer, TimerSidePanelSide.Right);

        Assert.AreEqual(1100f - 12f - right.Width, right.Left, "the trailing edge keeps the same 12 epx inset.");
        Assert.AreEqual(left.Top, right.Top);
        Assert.AreEqual(left.Height, right.Height);
        Assert.IsLessThan(0f, left.SlideOffset, "a left panel enters from off the leading edge.");
        Assert.IsGreaterThan(0f, right.SlideOffset, "a right panel enters from off the trailing edge.");
        Assert.AreEqual(-left.SlideOffset, right.SlideOffset, "both sides travel the same distance.");
    }

    [TestMethod]
    public void SlideOffset_ClearsTheCardAndItsInset()
    {
        TimerSidePanelLayout layout = TimerSidePanelLayoutResolver.Resolve(1100d, 720d, TitleBar, 69f, TimerSidePanelSide.Left);

        Assert.IsLessThanOrEqualTo(
            -(layout.Width + TimerSidePanelLayoutResolver.Inset),
            layout.SlideOffset,
            "the hidden card is fully off-screen, shadow included.");
    }

    [TestMethod]
    public void NarrowWindow_ShrinksTheCardRatherThanOverflowing()
    {
        TimerSidePanelLayout layout = TimerSidePanelLayoutResolver.Resolve(300d, 720d, TitleBar, 69f, TimerSidePanelSide.Right);

        Assert.AreEqual(300f - (2f * TimerSidePanelLayoutResolver.Inset), layout.Width, 0.01f);
        Assert.AreEqual(12f, layout.Left, 0.01f, "a shrunk card still keeps both insets.");
    }

    [TestMethod]
    public void CanShow_IsFalseWhenTheBandIsTooShort()
    {
        Assert.IsTrue(TimerSidePanelLayoutResolver.CanShow(1100d, 720d, TitleBar, 69f));
        Assert.IsFalse(
            TimerSidePanelLayoutResolver.CanShow(1100d, 240d, TitleBar, 69f),
            "a window whose band cannot hold a usable list refuses to open a panel.");
    }

    [TestMethod]
    public void CanShow_IsFalseWhenTheWindowIsNarrowerThanTheInsets()
    {
        Assert.IsFalse(TimerSidePanelLayoutResolver.CanShow(20d, 720d, TitleBar, 69f));
    }

    [TestMethod]
    public void Resolve_NeverProducesNegativeGeometry()
    {
        TimerSidePanelLayout layout = TimerSidePanelLayoutResolver.Resolve(0d, 0d, TitleBar, 69f, TimerSidePanelSide.Left);

        Assert.AreEqual(0f, layout.Width);
        Assert.AreEqual(0f, layout.Height);
    }
}
