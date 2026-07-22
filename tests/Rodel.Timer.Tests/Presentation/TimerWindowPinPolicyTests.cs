using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerWindowPinPolicyTests
{
    [TestMethod]
    public void UnpinnedNormalWindow_RetainsStandardCapabilitiesAndCommands()
    {
        TimerWindowPinBehavior behavior = TimerWindowPinPolicy.Resolve(
            isPinned: false,
            isMaximized: false);

        Assert.IsFalse(behavior.IsAlwaysOnTop);
        Assert.IsTrue(behavior.IsResizable);
        Assert.IsTrue(behavior.IsMaximizable);
        Assert.IsTrue(behavior.ShowCompactOverlayButton);
        Assert.IsTrue(behavior.ShowPinButton);
    }

    [TestMethod]
    public void PinnedWindow_IsTopmostResizableAndCannotMaximizeOrEnterCompactOverlay()
    {
        TimerWindowPinBehavior behavior = TimerWindowPinPolicy.Resolve(
            isPinned: true,
            isMaximized: false);

        Assert.IsTrue(behavior.IsAlwaysOnTop);
        Assert.IsTrue(behavior.IsResizable);
        Assert.IsFalse(behavior.IsMaximizable);
        Assert.IsFalse(behavior.ShowCompactOverlayButton);
        Assert.IsTrue(behavior.ShowPinButton);
    }

    [TestMethod]
    public void MaximizedWindow_HidesOnlyThePinCommand()
    {
        TimerWindowPinBehavior behavior = TimerWindowPinPolicy.Resolve(
            isPinned: false,
            isMaximized: true);

        Assert.IsFalse(behavior.ShowPinButton);
        Assert.IsTrue(behavior.ShowCompactOverlayButton);
    }
}
