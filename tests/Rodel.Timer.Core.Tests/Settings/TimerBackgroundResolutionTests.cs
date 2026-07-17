using Rodel.Timer.Core.Settings;

namespace Rodel.Timer.Core.Tests.Settings;

[TestClass]
public sealed class TimerBackgroundResolutionTests
{
    [TestMethod]
    public void Resolve_ActiveMaterial_PreservesEveryMaterialChoice()
    {
        TimerBackgroundMode[] materials =
        [
            TimerBackgroundMode.Mica,
            TimerBackgroundMode.MicaAlt,
            TimerBackgroundMode.Acrylic,
            TimerBackgroundMode.AcrylicThin,
        ];

        foreach (TimerBackgroundMode material in materials)
        {
            TimerBackgroundResolution resolution = TimerBackgroundResolution.Resolve(
                material,
                isMaterialActive: true);

            Assert.AreEqual(material, resolution.Requested);
            Assert.IsFalse(resolution.IsFallback);
        }
    }

    [TestMethod]
    public void Resolve_InactiveMaterial_UsesSolidFallback()
    {
        TimerBackgroundMode[] materials =
        [
            TimerBackgroundMode.Mica,
            TimerBackgroundMode.MicaAlt,
            TimerBackgroundMode.Acrylic,
            TimerBackgroundMode.AcrylicThin,
        ];

        foreach (TimerBackgroundMode material in materials)
        {
            TimerBackgroundResolution resolution = TimerBackgroundResolution.Resolve(
                material,
                isMaterialActive: false);

            Assert.AreEqual(material, resolution.Requested);
            Assert.IsTrue(resolution.IsFallback);
        }
    }

    [TestMethod]
    public void Resolve_UndefinedMode_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TimerBackgroundResolution.Resolve((TimerBackgroundMode)99, isMaterialActive: true));
}
