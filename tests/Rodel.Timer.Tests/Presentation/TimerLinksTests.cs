using System.Xml.Linq;

using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of the settings "open source" link. Rodel Timer is open source, so the link must point at the app's own
/// public repository (<c>Richasy/Rodel.Timer</c>), and the localized help must name the Rodel Timer source rather than
/// the archived Work-Timer predecessor. The strings are read from the linked <c>.resw</c> tables, so no reference to the
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
    public void OpenSourceUri_IsThePublicRodelTimerRepository()
    {
        // Constructing the Uri at runtime pins the exact link (scheme + host + path) without comparing two compile-time
        // constants (which the analyzer would fold to an always-true assertion).
        var uri = new Uri(TimerLinks.OpenSourceUri);

        Assert.AreEqual(Uri.UriSchemeHttps, uri.Scheme);
        Assert.AreEqual("github.com", uri.Host);
        Assert.AreEqual("/Richasy/Rodel.Timer", uri.AbsolutePath);
    }

    [TestMethod]
    public void OpenSourceHelp_NamesTheRodelTimerSource_InBothLanguages()
    {
        foreach (string language in new[] { "en-US", "zh-CN" })
        {
            Dictionary<string, string> table = Load(language);

            Assert.IsTrue(table.TryGetValue("OpenSourceHelp", out string? help), $"{language} is missing 'OpenSourceHelp'.");

            // The help must name the Rodel Timer source on GitHub (the public repo the link opens)...
            Assert.Contains("GitHub", help!, StringComparison.Ordinal);
            Assert.Contains("Rodel Timer", help!, StringComparison.Ordinal);

            // ...and must not still describe the archived Work-Timer predecessor.
            Assert.DoesNotContain("Work-Timer", help!, StringComparison.Ordinal);
        }
    }
}
