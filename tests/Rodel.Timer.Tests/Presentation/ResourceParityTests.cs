using System.Xml.Linq;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Parity coverage of the localized string tables: every supported language must expose exactly the same set of keys,
/// with no empty translation. The <c>.resw</c> files
/// are copied into the test output as linked content so the test runs from the build directory.
/// </summary>
[TestClass]
public sealed class ResourceParityTests
{
    private static readonly string[] Languages = ["en-US", "zh-CN", "zh-Hant"];

    private static Dictionary<string, string> Load(string language)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Strings", language, "Resources.resw");
        Assert.IsTrue(File.Exists(path), $"missing resource file: {path}");

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (XElement data in XDocument.Load(path).Root!.Elements("data"))
        {
            string? name = data.Attribute("name")?.Value;
            if (name is not null)
            {
                map[name] = data.Element("value")?.Value ?? string.Empty;
            }
        }

        return map;
    }

    [TestMethod]
    public void EveryLanguageHasTheSameKeysAsEnglish()
    {
        IReadOnlyDictionary<string, string> english = Load("en-US");
        foreach (string language in Languages[1..])
        {
            IReadOnlyDictionary<string, string> localized = Load(language);
            string[] missingInLocalized = english.Keys.Except(localized.Keys, StringComparer.Ordinal).OrderBy(static k => k).ToArray();
            string[] missingInEnglish = localized.Keys.Except(english.Keys, StringComparer.Ordinal).OrderBy(static k => k).ToArray();

            Assert.IsEmpty(missingInLocalized, $"keys missing from {language}: {string.Join(", ", missingInLocalized)}");
            Assert.IsEmpty(missingInEnglish, $"extra keys in {language}: {string.Join(", ", missingInEnglish)}");
        }
    }

    [TestMethod]
    public void NoTranslationIsEmpty()
    {
        foreach (string language in Languages)
        {
            foreach ((string key, string value) in Load(language))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"{language} value for '{key}' is empty.");
            }
        }
    }

    [TestMethod]
    public void EveryTableIsNonTrivialAndComplete()
    {
        // Guards against an accidentally emptied resource file silently passing the parity check.
        int englishCount = Load("en-US").Count;
        Assert.IsGreaterThanOrEqualTo(50, englishCount, "the English table should carry the full product string set.");
        foreach (string language in Languages[1..])
        {
            Assert.HasCount(englishCount, Load(language));
        }
    }
}
