using System.Globalization;

using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Unit coverage of <see cref="LanguageResolver"/> — the pure map between a stored language preference (a concrete tag
/// or the follow-system sentinel) and the effective concrete language, and the OS-culture → supported-language
/// resolution shared by the startup and runtime "follow system" paths.
/// </summary>
[TestClass]
public sealed class LanguageResolverTests
{
    [TestMethod]
    public void IsFollowSystem_TrueOnlyForTheSentinel()
    {
        Assert.IsTrue(LanguageResolver.IsFollowSystem(TimerPreferences.SystemLanguage));
        Assert.IsTrue(LanguageResolver.IsFollowSystem("SYSTEM"), "the check is case-insensitive.");
        Assert.IsFalse(LanguageResolver.IsFollowSystem(TimerPreferences.EnglishLanguage));
        Assert.IsFalse(LanguageResolver.IsFollowSystem(TimerPreferences.ChineseLanguage));
        Assert.IsFalse(LanguageResolver.IsFollowSystem(null));
    }

    [TestMethod]
    [DataRow("zh-Hant", TimerPreferences.TraditionalChineseLanguage)]
    [DataRow("zh-TW", TimerPreferences.TraditionalChineseLanguage)]
    [DataRow("zh-HK", TimerPreferences.TraditionalChineseLanguage)]
    [DataRow("zh-Hans", TimerPreferences.ChineseLanguage)]
    [DataRow("zh-Hans-CN", TimerPreferences.ChineseLanguage)]
    [DataRow("zh-CN", TimerPreferences.ChineseLanguage)]
    [DataRow("zh-SG", TimerPreferences.ChineseLanguage)]
    [DataRow("zh", TimerPreferences.ChineseLanguage)]
    [DataRow("en-US", TimerPreferences.EnglishLanguage)]
    [DataRow("en-GB", TimerPreferences.EnglishLanguage)]
    [DataRow("fr-FR", TimerPreferences.EnglishLanguage)]
    [DataRow("ja-JP", TimerPreferences.EnglishLanguage)]
    public void ResolveSupported_MapsOsCultureToTheNearestSupportedLanguage(string tag, string expected)
    {
        string resolved = LanguageResolver.ResolveSupported(CultureInfo.GetCultureInfo(tag));
        Assert.AreEqual(expected, resolved);
    }

    [TestMethod]
    public void ResolveSupported_NullCulture_Throws() =>
        Assert.ThrowsExactly<ArgumentNullException>(static () => LanguageResolver.ResolveSupported(null!));

    [TestMethod]
    public void ResolveEffective_FollowSystem_ResolvesTheOsCulture()
    {
        string effective = LanguageResolver.ResolveEffective(
            TimerPreferences.SystemLanguage,
            CultureInfo.GetCultureInfo("zh-TW"));

        Assert.AreEqual(TimerPreferences.TraditionalChineseLanguage, effective);
    }

    [TestMethod]
    public void ResolveEffective_ConcretePreference_IsReturnedUnchanged_IgnoringTheOsCulture()
    {
        string effective = LanguageResolver.ResolveEffective(
            TimerPreferences.EnglishLanguage,
            CultureInfo.GetCultureInfo("zh-CN"));

        Assert.AreEqual(TimerPreferences.EnglishLanguage, effective, "a pinned concrete language ignores the OS culture.");
    }
}
