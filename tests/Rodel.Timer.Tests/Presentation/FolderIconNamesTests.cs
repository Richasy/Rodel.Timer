using System.Xml.Linq;

using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of the localized folder-icon display names (the icon-picker accessibility fix: every cell must expose a
/// human-readable, localized <c>Name</c> instead of the raw <c>TimerFolderIcon</c> enum text, and the selected cell must
/// carry its own dedicated status string rather than reusing the folder-row "current folder" status). The names live in
/// the app resource tables under the <c>FolderIcon&lt;Name&gt;</c> key convention that <c>FolderIconNames.Resolve</c>
/// switches over; these tests read the linked <c>.resw</c> tables so they need no reference to the WinExe app assembly.
/// </summary>
[TestClass]
public sealed class FolderIconNamesTests
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

    private static string Key(FolderIconOption option) => $"FolderIcon{option.Icon}";

    [TestMethod]
    public void EveryCatalogIconHasANonEmptyEnglishAndChineseName()
    {
        Dictionary<string, string> english = Load("en-US");
        Dictionary<string, string> chinese = Load("zh-CN");

        foreach (FolderIconOption option in FolderIconCatalog.All)
        {
            string key = Key(option);
            Assert.IsTrue(english.TryGetValue(key, out string? en), $"en-US is missing icon name key '{key}'.");
            Assert.IsTrue(chinese.TryGetValue(key, out string? zh), $"zh-CN is missing icon name key '{key}'.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(en), $"en-US name for '{key}' is empty.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(zh), $"zh-CN name for '{key}' is empty.");
        }
    }

    [TestMethod]
    public void ChineseNamesAreLocalizedNotTheRawEnumText()
    {
        Dictionary<string, string> chinese = Load("zh-CN");

        foreach (FolderIconOption option in FolderIconCatalog.All)
        {
            string raw = option.Icon.ToString();
            string zh = chinese[Key(option)];
            Assert.AreNotEqual(raw, zh, $"zh-CN name for '{raw}' is still the raw enum text.");
        }
    }

    [TestMethod]
    public void SelectedIconStatus_IsItsOwnKey_NotTheFolderRowStatus()
    {
        foreach (string language in new[] { "en-US", "zh-CN" })
        {
            Dictionary<string, string> table = Load(language);

            Assert.IsTrue(table.TryGetValue("SelectedIconStatus", out string? status), $"{language} is missing 'SelectedIconStatus'.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(status), $"{language} 'SelectedIconStatus' is empty.");

            // The icon-picker selected status must be distinct from the folder-row "current folder" status it used to reuse.
            Assert.AreNotEqual(table["CurrentFolder"], status, $"{language} icon status still reuses CurrentFolder.");
        }
    }
}
