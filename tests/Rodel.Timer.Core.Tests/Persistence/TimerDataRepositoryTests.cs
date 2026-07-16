using System.Text.Json;

using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Tests.TestSupport;
using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Tests.Persistence;

[TestClass]
public sealed class TimerDataRepositoryTests
{
    [TestMethod]
    public async Task SaveAndLoad_RoundTripsTheWholeWorkspace()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        using var repository = new TimerDataRepository(path);
        TimerWorkspace workspace = CreateWorkspace();

        await repository.SaveAsync(workspace);
        TimerWorkspace? loaded = await repository.LoadAsync();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(workspace.SelectedFolderId, loaded.SelectedFolderId);
        Assert.HasCount(2, loaded.Folders);
        Assert.AreEqual(TimerFolderIcon.Book, loaded.Folders[1].Icon);
        Assert.HasCount(1, loaded.Entries);
        Assert.AreEqual("Read", loaded.Entries[0].Title);
        Assert.AreEqual(300L, loaded.Entries[0].DurationSeconds);
        Assert.AreEqual(TimerThemeMode.Dark, loaded.Preferences.Theme);
        Assert.AreEqual(TimerBackgroundMode.Acrylic, loaded.Preferences.Background);

        string json = await File.ReadAllTextAsync(path);
        StringAssert.Contains(json, "\"schemaVersion\": 2");
        StringAssert.Contains(json, "\"background\": \"Acrylic\"");
        StringAssert.Contains(json, "\"icon\": \"Book\"");
        Assert.IsEmpty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [TestMethod]
    public async Task Load_SchemaVersion1_ExplicitlyMigratesBackgroundToSystemMaterial()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "schemaVersion": 1,
              "selectedFolderId": "11111111-1111-1111-1111-111111111111",
              "preferences": {
                "theme": "Dark",
                "language": "en-US",
                "fontFamily": "Segoe UI",
                "dailyTargetMinutes": 360
              },
              "folders": [
                {
                  "id": "11111111-1111-1111-1111-111111111111",
                  "name": "Work",
                  "createdAt": "2026-07-14T00:00:00+00:00",
                  "icon": "Briefcase"
                }
              ],
              "entries": []
            }
            """);
        using var repository = new TimerDataRepository(path);

        TimerWorkspace? loaded = await repository.LoadAsync();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(
            TimerBackgroundMode.SystemMaterial,
            loaded.Preferences.Background,
            "schema v1 explicitly migrates to System Material rather than relying on an enum default.");

        await repository.SaveAsync(loaded);
        string upgraded = await File.ReadAllTextAsync(path);
        StringAssert.Contains(upgraded, "\"schemaVersion\": 2");
        StringAssert.Contains(upgraded, "\"background\": \"SystemMaterial\"");
    }

    [TestMethod]
    public async Task Load_SchemaVersion2WithoutBackground_IsCorrupt()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "schemaVersion": 2,
              "selectedFolderId": "11111111-1111-1111-1111-111111111111",
              "preferences": {
                "theme": "System",
                "language": "en-US",
                "fontFamily": "Segoe UI",
                "dailyTargetMinutes": 360
              },
              "folders": [],
              "entries": []
            }
            """);
        using var repository = new TimerDataRepository(path);

        await Assert.ThrowsAsync<TimerDataCorruptedException>(() => repository.LoadAsync());
    }

    [TestMethod]
    public async Task Load_ReportsCorruptJsonWithoutReplacingTheFile()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        await File.WriteAllTextAsync(path, "{ not-json");
        using var repository = new TimerDataRepository(path);

        await Assert.ThrowsAsync<TimerDataCorruptedException>(
            () => repository.LoadAsync());

        Assert.AreEqual("{ not-json", await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task Load_ReportsNullItemsAsCorruptData()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "schemaVersion": 1,
              "selectedFolderId": "11111111-1111-1111-1111-111111111111",
              "preferences": {
                "theme": "System",
                "language": "en-US",
                "fontFamily": "Segoe UI",
                "dailyTargetMinutes": 360
              },
              "folders": [null],
              "entries": []
            }
            """);
        using var repository = new TimerDataRepository(path);

        await Assert.ThrowsAsync<TimerDataCorruptedException>(
            () => repository.LoadAsync());
    }

    [TestMethod]
    public async Task Save_ReplacesACompletePreviousDocument()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        using var repository = new TimerDataRepository(path);
        TimerWorkspace workspace = CreateWorkspace();
        await repository.SaveAsync(workspace);
        workspace.AddEntry(new TimerEntry(
            Guid.NewGuid(),
            workspace.SelectedFolderId,
            "Second",
            42,
            new DateTimeOffset(2026, 7, 14, 2, 0, 0, TimeSpan.Zero)));

        await repository.SaveAsync(workspace);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.AreEqual(
            2,
            document.RootElement.GetProperty("entries").GetArrayLength());
        Assert.IsEmpty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    private static TimerWorkspace CreateWorkspace()
    {
        var first = new TimerFolder(
            Guid.ParseExact("11111111111111111111111111111111", "N"),
            "Work",
            DateTimeOffset.UnixEpoch,
            TimerFolderIcon.Activity);
        var second = new TimerFolder(
            Guid.ParseExact("22222222222222222222222222222222", "N"),
            "Reading",
            DateTimeOffset.UnixEpoch.AddDays(1),
            TimerFolderIcon.Book);
        var preferences = new TimerPreferences(
            TimerThemeMode.Dark,
            TimerBackgroundMode.Acrylic,
            TimerPreferences.EnglishLanguage,
            "Segoe UI",
            480,
            "2.0.0");
        TimerWorkspace workspace = TimerWorkspace.Create(first, preferences);
        workspace.AddFolder(second);
        workspace.SelectFolder(second.Id);
        workspace.AddEntry(new TimerEntry(
            Guid.ParseExact("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "N"),
            second.Id,
            "Read",
            300,
            new DateTimeOffset(2026, 7, 14, 1, 0, 0, TimeSpan.Zero)));
        return workspace;
    }
}
