using System.Globalization;
using System.Xml.Linq;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of the history row's accessible-description resource (finding: a history "more" button must expose the
/// entry's title, duration, and timestamp so a screen-reader user hears every visible fact, not just the title). The
/// <c>HistoryItemAccessibleDescription</c> key is what <c>MainView.BuildHistoryRow</c> formats into the button's
/// <c>HelpText</c> / <c>FullDescription</c>; this test pins that the localized format carries all three fields, in both
/// languages, without hard-coding the surrounding wording.
/// </summary>
[TestClass]
public sealed class HistoryRowDescriptionTests
{
    private const string Key = "HistoryItemAccessibleDescription";

    private static string Format(string language)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Strings", language, "Resources.resw");
        Assert.IsTrue(File.Exists(path), $"missing resource file: {path}");

        foreach (XElement data in XDocument.Load(path).Root!.Elements("data"))
        {
            if (string.Equals(data.Attribute("name")?.Value, Key, StringComparison.Ordinal))
            {
                return data.Element("value")?.Value ?? string.Empty;
            }
        }

        Assert.Fail($"resource key '{Key}' is missing from {language}.");
        return string.Empty;
    }

    [TestMethod]
    [DataRow("en-US")]
    [DataRow("zh-CN")]
    public void AccessibleDescription_CarriesTitleDurationAndTimestamp(string language)
    {
        string format = Format(language);

        Assert.Contains("{0}", format, StringComparison.Ordinal);
        Assert.Contains("{1}", format, StringComparison.Ordinal);
        Assert.Contains("{2}", format, StringComparison.Ordinal);

        string rendered = string.Format(CultureInfo.InvariantCulture, format, "Deep work", "01:23:45", "2026-07-14 09:00");

        Assert.Contains("Deep work", rendered, StringComparison.Ordinal);
        Assert.Contains("01:23:45", rendered, StringComparison.Ordinal);
        Assert.Contains("2026-07-14 09:00", rendered, StringComparison.Ordinal);
    }
}
