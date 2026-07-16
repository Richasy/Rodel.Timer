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
            TimerBackgroundMode.SystemMaterial,
            TimerBackgroundMode.Acrylic,
            TimerBackgroundMode.LiquidGlassWallpaper,
        ];

        foreach (TimerBackgroundMode material in materials)
        {
            TimerBackgroundResolution resolution = TimerBackgroundResolution.Resolve(
                material,
                isMaterialActive: true);

            Assert.AreEqual(material, resolution.Requested);
            Assert.AreEqual(material, resolution.Effective);
            Assert.IsFalse(resolution.IsFallback);
        }
    }

    [TestMethod]
    public void Resolve_InactiveMaterial_UsesSolidFallback()
    {
        TimerBackgroundMode[] materials =
        [
            TimerBackgroundMode.SystemMaterial,
            TimerBackgroundMode.Acrylic,
            TimerBackgroundMode.LiquidGlassWallpaper,
        ];

        foreach (TimerBackgroundMode material in materials)
        {
            TimerBackgroundResolution resolution = TimerBackgroundResolution.Resolve(
                material,
                isMaterialActive: false);

            Assert.AreEqual(material, resolution.Requested);
            Assert.AreEqual(TimerBackgroundMode.Solid, resolution.Effective);
            Assert.IsTrue(resolution.IsFallback);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Resolve_Solid_IgnoresMaterialActivity(bool isMaterialActive)
    {
        TimerBackgroundResolution resolution = TimerBackgroundResolution.Resolve(
            TimerBackgroundMode.Solid,
            isMaterialActive);

        Assert.AreEqual(TimerBackgroundMode.Solid, resolution.Effective);
        Assert.IsFalse(resolution.IsFallback);
    }

    [TestMethod]
    public void Resolve_UndefinedMode_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TimerBackgroundResolution.Resolve((TimerBackgroundMode)99, isMaterialActive: true));
}
