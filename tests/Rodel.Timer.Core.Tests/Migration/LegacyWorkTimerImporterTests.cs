using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Settings;

namespace Rodel.Timer.Core.Tests.Migration;

[TestClass]
public sealed class LegacyWorkTimerImporterTests
{
    private const string FolderId = "11111111111111111111111111111111";
    private const string SecondFolderId = "22222222222222222222222222222222";
    private const string EntryId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [TestMethod]
    public void Import_PreservesLegacyFoldersHistoryAndSettings()
    {
        var snapshot = new LegacyWorkTimerSnapshot(
            $$"""
            [
              {
                "Name": "Reading",
                "CreateTime": "2022-01-02T03:04:05",
                "Id": "{{FolderId}}",
                "Icon": 59756
              },
              {
                "Name": "Work",
                "CreateTime": "2022-01-03T03:04:05",
                "Id": "{{SecondFolderId}}",
                "Icon": 59759
              }
            ]
            """,
            $$"""
            [
              {
                "Name": "Chapter 1",
                "FolderId": "{{FolderId}}",
                "Id": "{{EntryId}}",
                "TotalSeconds": 125,
                "CreateTime": "2022-01-02T04:05:06"
              }
            ]
            """,
            new Dictionary<string, string>
            {
                ["Theme"] = "Dark",
                ["Language"] = "zh_CN",
                ["FontFamily"] = "Microsoft YaHei UI",
                ["LastSelectFolderId"] = SecondFolderId,
                ["PresetDuration"] = "480",
                ["AppVersion"] = "1.0.4",
            });
        LegacyWorkTimerImporter importer = CreateImporter();

        LegacyImportResult result = importer.Import(
            snapshot,
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        Assert.HasCount(2, result.Workspace.Folders);
        Assert.AreEqual(TimerFolderIcon.Folder, result.Workspace.Folders[0].Icon);
        Assert.AreEqual(TimerFolderIcon.Framer, result.Workspace.Folders[1].Icon);
        Assert.HasCount(1, result.Workspace.Entries);
        Assert.AreEqual("Chapter 1", result.Workspace.Entries[0].Title);
        Assert.AreEqual(125L, result.Workspace.Entries[0].DurationSeconds);
        Assert.AreEqual(
            Guid.ParseExact(SecondFolderId, "N"),
            result.Workspace.SelectedFolderId);
        Assert.AreEqual(TimerThemeMode.Dark, result.Workspace.Preferences.Theme);
        Assert.AreEqual(
            TimerPreferences.ChineseLanguage,
            result.Workspace.Preferences.Language);
        Assert.AreEqual(480, result.Workspace.Preferences.DailyTargetMinutes);
        Assert.AreEqual("1.0.4", result.Workspace.Preferences.LastSeenVersion);
        Assert.IsEmpty(result.Warnings);
    }

    [TestMethod]
    public void Import_ReplacesUnknownIconAndReportsAWarning()
    {
        var snapshot = new LegacyWorkTimerSnapshot(
            $$"""
            [
              {
                "Name": "Unknown",
                "CreateTime": "2022-01-02T03:04:05Z",
                "Id": "{{FolderId}}",
                "Icon": 1
              }
            ]
            """,
            "[]",
            new Dictionary<string, string>());

        LegacyImportResult result = CreateImporter().Import(
            snapshot,
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        Assert.AreEqual(TimerFolderIcon.Activity, result.Workspace.Folders[0].Icon);
        Assert.HasCount(1, result.Warnings);
        StringAssert.Contains(result.Warnings[0], "U+0001");
    }

    [TestMethod]
    public void Import_RehomesHistoryThatReferencesAMissingFolder()
    {
        var snapshot = new LegacyWorkTimerSnapshot(
            $$"""
            [
              {
                "Name": "Reading",
                "CreateTime": "2022-01-02T03:04:05Z",
                "Id": "{{FolderId}}",
                "Icon": 59648
              }
            ]
            """,
            $$"""
            [
              {
                "Name": "Orphan",
                "FolderId": "{{SecondFolderId}}",
                "Id": "{{EntryId}}",
                "TotalSeconds": 5,
                "CreateTime": "2022-01-02T04:05:06Z"
              }
            ]
            """,
            new Dictionary<string, string>());

        LegacyImportResult result = CreateImporter().Import(
            snapshot,
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch);

        Assert.AreEqual(
            Guid.ParseExact(FolderId, "N"),
            result.Workspace.Entries[0].FolderId);
        Assert.HasCount(1, result.Warnings);
        StringAssert.Contains(result.Warnings[0], "missing folder");
    }

    [TestMethod]
    public void Import_CreatesFallbackFolderForSettingsOnlySnapshot()
    {
        Guid fallbackId = Guid.NewGuid();
        var snapshot = new LegacyWorkTimerSnapshot(
            null,
            null,
            new Dictionary<string, string>
            {
                ["PresetDuration"] = "720",
            });

        LegacyImportResult result = CreateImporter().Import(
            snapshot,
            fallbackId,
            new DateTimeOffset(2026, 7, 14, 1, 2, 3, TimeSpan.Zero));

        Assert.HasCount(1, result.Workspace.Folders);
        Assert.AreEqual(fallbackId, result.Workspace.Folders[0].Id);
        Assert.AreEqual("Default", result.Workspace.Folders[0].Name);
        Assert.AreEqual(720, result.Workspace.Preferences.DailyTargetMinutes);
    }

    [TestMethod]
    public void Import_RecoversPlausibleWallClockAndBlankFieldDamage()
    {
        DateTimeOffset importedAt = new(2026, 7, 14, 1, 2, 3, TimeSpan.Zero);
        var snapshot = new LegacyWorkTimerSnapshot(
            $$"""
            [
              {
                "Name": " ",
                "CreateTime": "0001-01-01T00:00:00",
                "Id": "{{FolderId}}",
                "Icon": 59648
              }
            ]
            """,
            $$"""
            [
              {
                "Name": "",
                "FolderId": "{{FolderId}}",
                "Id": "{{EntryId}}",
                "TotalSeconds": -30,
                "CreateTime": "0001-01-01T00:00:00"
              }
            ]
            """,
            new Dictionary<string, string>());

        LegacyImportResult result = CreateImporter().Import(
            snapshot,
            Guid.NewGuid(),
            importedAt);

        Assert.AreEqual("Default 1", result.Workspace.Folders[0].Name);
        Assert.AreEqual(importedAt, result.Workspace.Folders[0].CreatedAt);
        Assert.AreEqual("Untitled", result.Workspace.Entries[0].Title);
        Assert.AreEqual(0L, result.Workspace.Entries[0].DurationSeconds);
        Assert.AreEqual(importedAt, result.Workspace.Entries[0].CreatedAt);
        Assert.HasCount(5, result.Warnings);
    }

    [TestMethod]
    public void Import_RejectsNullArrayItemsAsStructuralDamage()
    {
        var snapshot = new LegacyWorkTimerSnapshot(
            "[null]",
            "[]",
            new Dictionary<string, string>());

        Assert.Throws<LegacyDataMigrationException>(
            () => CreateImporter().Import(
                snapshot,
                Guid.NewGuid(),
                DateTimeOffset.UnixEpoch));
    }

    private static LegacyWorkTimerImporter CreateImporter() => new(
        "Default",
        TimerPreferences.Default,
        TimeZoneInfo.Utc);
}
