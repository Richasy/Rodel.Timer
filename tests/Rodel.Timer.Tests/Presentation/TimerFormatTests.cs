using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Pure-function coverage of <see cref="TimerFormat"/> — the culture-invariant <c>HH:mm:ss</c> clock and the
/// whole-minute conversion used for the daily-target summary.
/// </summary>
[TestClass]
public sealed class TimerFormatTests
{
    [TestMethod]
    public void Clock_ZeroPadsHoursMinutesSeconds()
    {
        Assert.AreEqual("00:00:00", TimerFormat.Clock(TimeSpan.Zero));
        Assert.AreEqual("01:05:03", TimerFormat.Clock(new TimeSpan(1, 5, 3)));
        Assert.AreEqual("00:00:09", TimerFormat.Clock(TimeSpan.FromSeconds(9)));
    }

    [TestMethod]
    public void Clock_HoursAreNotCappedAtTwentyFour()
    {
        Assert.AreEqual("26:00:00", TimerFormat.Clock(TimeSpan.FromHours(26)));
        Assert.AreEqual("100:01:01", TimerFormat.Clock(new TimeSpan(100, 1, 1)));
    }

    [TestMethod]
    public void Clock_NegativeDurationsClampToZero()
    {
        Assert.AreEqual("00:00:00", TimerFormat.Clock(TimeSpan.FromSeconds(-5)));
        Assert.AreEqual("00:00:00", TimerFormat.Clock(TimeSpan.FromHours(-3)));
    }

    [TestMethod]
    public void Clock_TruncatesSubSecondFractions()
    {
        Assert.AreEqual("00:00:01", TimerFormat.Clock(TimeSpan.FromMilliseconds(1999)));
    }

    [TestMethod]
    public void WholeMinutes_RoundsDownAndNeverNegative()
    {
        Assert.AreEqual(0, TimerFormat.WholeMinutes(TimeSpan.Zero));
        Assert.AreEqual(1, TimerFormat.WholeMinutes(TimeSpan.FromSeconds(119)));
        Assert.AreEqual(90, TimerFormat.WholeMinutes(TimeSpan.FromMinutes(90)));
        Assert.AreEqual(0, TimerFormat.WholeMinutes(TimeSpan.FromMinutes(-5)));
    }
}
