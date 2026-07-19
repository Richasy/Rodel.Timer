using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Graphics;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerWindowPlacementPolicyTests
{
    private static readonly Size CompactMaximum = new(960f, 540f);

    [TestMethod]
    public void ForPresenter_UsesIndependentSlots()
    {
        Assert.AreEqual(
            TimerWindowPlacementMode.Overlapped,
            TimerWindowPlacementPolicy.ForPresenter(WindowPresenterKind.Overlapped));
        Assert.AreEqual(
            TimerWindowPlacementMode.CompactOverlay,
            TimerWindowPlacementPolicy.ForPresenter(WindowPresenterKind.CompactOverlay));
    }

    [TestMethod]
    [DataRow(300, 150, 96, TimerWindowPlacementMode.CompactOverlay)]
    [DataRow(700, 500, 96, TimerWindowPlacementMode.CompactOverlay)]
    [DataRow(1920, 1080, 192, TimerWindowPlacementMode.CompactOverlay)]
    [DataRow(961, 500, 96, TimerWindowPlacementMode.Overlapped)]
    [DataRow(900, 541, 96, TimerWindowPlacementMode.Overlapped)]
    [DataRow(1922, 1000, 192, TimerWindowPlacementMode.Overlapped)]
    public void ClassifyLegacy_UsesEffectiveSize(
        int width,
        int height,
        int dpi,
        TimerWindowPlacementMode expected)
    {
        var placement = new WindowPlacement(
            100,
            100,
            width,
            height,
            (uint)dpi,
            isMaximized: false);

        Assert.AreEqual(
            expected,
            TimerWindowPlacementPolicy.ClassifyLegacy(placement, CompactMaximum));
    }

    [TestMethod]
    public void ClassifyLegacy_MaximizedPlacementIsAlwaysOverlapped()
    {
        var placement = new WindowPlacement(
            100,
            100,
            width: 300,
            height: 150,
            dpi: 96,
            isMaximized: true);

        Assert.AreEqual(
            TimerWindowPlacementMode.Overlapped,
            TimerWindowPlacementPolicy.ClassifyLegacy(placement, CompactMaximum));
    }
}
