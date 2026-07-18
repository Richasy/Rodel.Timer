using Rodel.Timer.Presentation;

using Sprout.Graphics;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerCompactLayoutTests
{
    [TestMethod]
    [DataRow(360d, 203d, TimerCompactMode.Balanced, 40f)]
    [DataRow(300d, 150d, TimerCompactMode.Tight, 31f)]
    [DataRow(480d, 180d, TimerCompactMode.Wide, 42f)]
    public void ApprovedMatrix_ResolvesExpectedState(
        double width,
        double height,
        TimerCompactMode expected,
        float clockSize)
    {
        TimerCompactLayout layout = TimerCompactLayoutResolver.Resolve(width, height);

        Assert.AreEqual(expected, layout.Mode);
        Assert.AreEqual(clockSize, layout.ClockSize);
        Assert.AreEqual(expected != TimerCompactMode.Tight, layout.ShowContext);
    }

    [TestMethod]
    public void Tight_IsStrictlyBelowEitherThreshold()
    {
        Assert.AreEqual(
            TimerCompactMode.Tight,
            TimerCompactLayoutResolver.Resolve(319.99d, 300d).Mode);
        Assert.AreEqual(
            TimerCompactMode.Tight,
            TimerCompactLayoutResolver.Resolve(500d, 157.99d).Mode);
        Assert.AreEqual(
            TimerCompactMode.Balanced,
            TimerCompactLayoutResolver.Resolve(320d, 220d).Mode);
    }

    [TestMethod]
    public void Wide_UsesInclusiveWidthAndExclusiveHeight()
    {
        Assert.AreEqual(
            TimerCompactMode.Wide,
            TimerCompactLayoutResolver.Resolve(420d, 219.99d).Mode);
        Assert.AreEqual(
            TimerCompactMode.Balanced,
            TimerCompactLayoutResolver.Resolve(419.99d, 180d).Mode);
        Assert.AreEqual(
            TimerCompactMode.Balanced,
            TimerCompactLayoutResolver.Resolve(480d, 220d).Mode);
    }

    [TestMethod]
    public void Tight_RemovesContextAndShrinksOnlyExplicitMetrics()
    {
        TimerCompactLayout tight = TimerCompactLayoutResolver.Resolve(300d, 150d);
        TimerCompactLayout balanced = TimerCompactLayoutResolver.Resolve(360d, 203d);

        Assert.IsFalse(tight.ShowContext);
        Assert.IsTrue(balanced.ShowContext);
        Assert.AreEqual(40f, tight.ActionSize);
        Assert.AreEqual(48f, balanced.ActionSize);
        Assert.AreEqual(45f, TimerCompactLayoutResolver.Resolve(480d, 180d).OuterHorizontalPadding);
        Assert.AreEqual(390f, tight.ContentMaxWidth);
        Assert.AreEqual(390f, balanced.ContentMaxWidth);
    }

    [TestMethod]
    public void MinimumClientSize_ResolvesToTightWithTheClockClearOfTheAction()
    {
        Size min = TimerCompactLayoutResolver.MinimumClientSize;
        TimerCompactLayout layout = TimerCompactLayoutResolver.Resolve(min.Width, min.Height);

        Assert.AreEqual(TimerCompactMode.Tight, layout.Mode);
        Assert.IsFalse(layout.ShowContext);

        float available = TimerCompactLayoutResolver.ClockAvailableWidth(layout, min.Width);
        float clock = TimerCompactLayoutResolver.EstimateClockWidth(layout.ClockSize);
        Assert.IsGreaterThanOrEqualTo(
            clock,
            available,
            $"At the enforced minimum {min.Width}×{min.Height}, the tight clock ({clock} epx) must fit its cell ({available} epx).");
    }

    [TestMethod]
    public void DefaultInitialSize_IsWithinTheOverlayResizeRange()
    {
        Size min = TimerCompactLayoutResolver.MinimumClientSize;
        Size initial = TimerCompactLayoutResolver.DefaultInitialSize;

        Assert.IsTrue(initial.Width >= min.Width && initial.Height >= min.Height);
    }

    [TestMethod]
    public void EveryValidClientSize_KeepsTheClockClearOfTheAction()
    {
        Size min = TimerCompactLayoutResolver.MinimumClientSize;

        // Sweep the whole user-resizable range (the framework compact-overlay cap is 960×540) at a fine step and prove
        // the clock cell always has at least its conservatively estimated width — so the clock never overlaps or is
        // clipped by the primary action in any tight/balanced/wide state the user can resize into.
        for (double width = min.Width; width <= 960d; width += 5d)
        {
            for (double height = min.Height; height <= 540d; height += 10d)
            {
                TimerCompactLayout layout = TimerCompactLayoutResolver.Resolve(width, height);
                float available = TimerCompactLayoutResolver.ClockAvailableWidth(layout, width);
                float clock = TimerCompactLayoutResolver.EstimateClockWidth(layout.ClockSize);

                Assert.IsGreaterThanOrEqualTo(
                    clock,
                    available,
                    $"At {width}×{height} ({layout.Mode}), the clock ({clock} epx) overflows its cell ({available} epx).");
            }
        }
    }
}
