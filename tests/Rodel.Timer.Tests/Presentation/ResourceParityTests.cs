using System.Xml.Linq;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Parity coverage of the localized string tables: the English (<c>en-US</c>) and Chinese (<c>zh-CN</c>) resource
/// files must expose exactly the same set of keys, with no empty translation on either side. The <c>.resw</c> files
/// are copied into the test output as linked content so the test runs from the build directory.
/// </summary>
[TestClass]
public sealed class ResourceParityTests
{
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
    public void EnglishAndChineseHaveTheSameKeys()
    {
        IReadOnlyDictionary<string, string> english = Load("en-US");
        IReadOnlyDictionary<string, string> chinese = Load("zh-CN");

        string[] missingInChinese = english.Keys.Except(chinese.Keys, StringComparer.Ordinal).OrderBy(static k => k).ToArray();
        string[] missingInEnglish = chinese.Keys.Except(english.Keys, StringComparer.Ordinal).OrderBy(static k => k).ToArray();

        Assert.IsEmpty(missingInChinese, $"keys missing from zh-CN: {string.Join(", ", missingInChinese)}");
        Assert.IsEmpty(missingInEnglish, $"keys missing from en-US: {string.Join(", ", missingInEnglish)}");
    }

    [TestMethod]
    public void NoTranslationIsEmpty()
    {
        foreach (string language in new[] { "en-US", "zh-CN" })
        {
            foreach ((string key, string value) in Load(language))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"{language} value for '{key}' is empty.");
            }
        }
    }

    [TestMethod]
    public void BothTablesAreNonTrivial()
    {
        // Guards against an accidentally emptied resource file silently passing the parity check.
        Assert.IsGreaterThanOrEqualTo(50, Load("en-US").Count, "the English table should carry the full product string set.");
        Assert.HasCount(Load("en-US").Count, Load("zh-CN"));
    }
}
