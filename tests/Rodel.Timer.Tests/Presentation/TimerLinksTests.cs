using System.Xml.Linq;

using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of the settings "open source" link. Rodel Timer's own source is private, so the link must point at the
/// public, archived Work-Timer repository (not a private <c>Rodel.Timer</c> repo that would 404 for store users), and
/// the localized label/help must describe it accurately as the archived Work-Timer source rather than claiming the
/// private Rodel source is public. The strings are read from the linked <c>.resw</c> tables, so no reference to the
/// WinExe app assembly is needed.
/// </summary>
[TestClass]
public sealed class TimerLinksTests
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
    public void OpenSourceUri_IsThePublicArchivedWorkTimerRepository()
    {
        // Constructing the Uri at runtime pins the exact link (scheme + host + path) without comparing two compile-time
        // constants (which the analyzer would fold to an always-true assertion).
        var uri = new Uri(TimerLinks.OpenSourceUri);

        Assert.AreEqual(Uri.UriSchemeHttps, uri.Scheme);
        Assert.AreEqual("github.com", uri.Host);
        Assert.AreEqual("/Richasy/Work-Timer", uri.AbsolutePath);
    }

    [TestMethod]
    public void OpenSourceLabelAndHelp_NameTheArchivedWorkTimerSource_InBothLanguages()
    {
        foreach (string language in new[] { "en-US", "zh-CN" })
        {
            Dictionary<string, string> table = Load(language);

            Assert.IsTrue(table.TryGetValue("OpenSource", out string? label), $"{language} is missing 'OpenSource'.");
            Assert.IsTrue(table.TryGetValue("OpenSourceHelp", out string? help), $"{language} is missing 'OpenSourceHelp'.");

            // The label/help must name the archived Work-Timer source (the public repo the link opens)...
            Assert.Contains("Work-Timer", label!, StringComparison.Ordinal);
            Assert.Contains("Work-Timer", help!, StringComparison.Ordinal);

            // ...and must not claim the private "Rodel Timer" / "Rodel 计时器" source is what opens on GitHub.
            Assert.DoesNotContain("Rodel Timer", help!, StringComparison.Ordinal);
            Assert.DoesNotContain("Rodel 计时器", help!, StringComparison.Ordinal);
        }
    }
}
