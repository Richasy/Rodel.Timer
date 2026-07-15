using System.Xml.Linq;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of the window-title source. The window caption is set from <c>Resources.AppName</c> in the current language
/// (at startup, on data-load, and centrally on a runtime language switch via <c>MainView.ApplyLanguage</c>), so the
/// caption tracks the language. This pins the invariant the title sync relies on — that <c>AppName</c> is present,
/// non-empty, and genuinely localized (English ≠ Chinese) — without a window; the live caption swap is verified
/// end-to-end by the Sprout.DevTools drive over the published exe.
/// </summary>
[TestClass]
public sealed class WindowTitleLocalizationTests
{
    private static string AppName(string language)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Strings", language, "Resources.resw");
        Assert.IsTrue(File.Exists(path), $"missing resource file: {path}");

        foreach (XElement data in XDocument.Load(path).Root!.Elements("data"))
        {
            if (string.Equals(data.Attribute("name")?.Value, "AppName", StringComparison.Ordinal))
            {
                return data.Element("value")?.Value ?? string.Empty;
            }
        }

        Assert.Fail($"AppName is missing from {language}.");
        return string.Empty;
    }

    [TestMethod]
    public void AppName_IsPresentAndNonEmptyInBothLanguages()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(AppName("en-US")));
        Assert.IsFalse(string.IsNullOrWhiteSpace(AppName("zh-CN")));
    }

    [TestMethod]
    public void AppName_IsLocalized_SoTheCaptionTracksLanguage() =>
        Assert.AreNotEqual(
            AppName("en-US"),
            AppName("zh-CN"),
            "AppName must differ by language so the window caption changes on a language switch.");
}
