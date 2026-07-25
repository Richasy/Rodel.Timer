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
        Assert.IsNull(loaded.Entries[0].Comment, "An entry saved without a comment round-trips as null.");
        Assert.AreEqual(TimerThemeMode.Dark, loaded.Preferences.Theme);
        Assert.AreEqual(TimerBackgroundMode.Acrylic, loaded.Preferences.Background);

        string json = await File.ReadAllTextAsync(path);
        StringAssert.Contains(json, "\"schemaVersion\": 3");
        StringAssert.Contains(json, "\"background\": \"Acrylic\"");
        StringAssert.Contains(json, "\"icon\": \"Book\"");
        Assert.IsEmpty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [TestMethod]
    public async Task SaveAndLoad_RoundTripsAnEntryComment()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        using var repository = new TimerDataRepository(path);
        var folder = new TimerFolder(
            Guid.ParseExact("11111111111111111111111111111111", "N"),
            "Work",
            DateTimeOffset.UnixEpoch,
            TimerFolderIcon.Activity);
        TimerWorkspace workspace = TimerWorkspace.Create(folder);
        workspace.AddEntry(new TimerEntry(
            Guid.ParseExact("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "N"),
            folder.Id,
            "Read",
            300,
            DateTimeOffset.UnixEpoch,
            "Chapter 4 notes"));

        await repository.SaveAsync(workspace);
        TimerWorkspace? loaded = await repository.LoadAsync();

        Assert.IsNotNull(loaded);
        Assert.AreEqual("Chapter 4 notes", loaded.Entries[0].Comment);
        StringAssert.Contains(await File.ReadAllTextAsync(path), "\"comment\": \"Chapter 4 notes\"");
    }

    [TestMethod]
    public async Task SaveAndLoad_RoundTripsEveryCurrentBackgroundMode()
    {
        using var directory = new TemporaryDirectory();

        foreach (TimerBackgroundMode background in Enum.GetValues<TimerBackgroundMode>())
        {
            string path = directory.File($"{background}.json");
            using var repository = new TimerDataRepository(path);
            TimerWorkspace workspace = CreateWorkspace(background);

            await repository.SaveAsync(workspace);
            TimerWorkspace? loaded = await repository.LoadAsync();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(background, loaded.Preferences.Background);
            StringAssert.Contains(
                await File.ReadAllTextAsync(path),
                $"\"background\": \"{background}\"");
        }
    }

    [TestMethod]
    public async Task Load_SchemaVersion1_ExplicitlyMigratesBackgroundToMica()
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
            TimerBackgroundMode.Mica,
            loaded.Preferences.Background,
            "Schema v1 explicitly migrates to Mica rather than relying on an enum default.");

        await repository.SaveAsync(loaded);
        string upgraded = await File.ReadAllTextAsync(path);
        StringAssert.Contains(upgraded, "\"schemaVersion\": 3");
        StringAssert.Contains(upgraded, "\"background\": \"Mica\"");
    }

    [TestMethod]
    public async Task Load_SchemaVersion2_ExplicitlyMigratesEveryLegacyBackgroundMode()
    {
        using var directory = new TemporaryDirectory();
        (string Persisted, TimerBackgroundMode Expected)[] cases =
        [
            ("SystemMaterial", TimerBackgroundMode.Mica),
            ("Solid", TimerBackgroundMode.Mica),
            ("Acrylic", TimerBackgroundMode.AcrylicThin),
            ("LiquidGlassWallpaper", TimerBackgroundMode.Mica),
        ];

        foreach ((string persisted, TimerBackgroundMode expected) in cases)
        {
            string path = directory.File($"{persisted}.json");
            await File.WriteAllTextAsync(path, SchemaVersion2Document(persisted));
            using var repository = new TimerDataRepository(path);

            TimerWorkspace? loaded = await repository.LoadAsync();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(expected, loaded.Preferences.Background, persisted);
        }
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
    public async Task Load_SchemaVersion3_RejectsLegacyAndNumericBackgroundTokens()
    {
        using var directory = new TemporaryDirectory();

        foreach (string persisted in new[] { "SystemMaterial", "Solid", "2", "Unknown" })
        {
            string path = directory.File($"{persisted}.json");
            await File.WriteAllTextAsync(path, SchemaVersion3Document(persisted));
            using var repository = new TimerDataRepository(path);

            await Assert.ThrowsAsync<TimerDataCorruptedException>(() => repository.LoadAsync());
        }
    }

    [TestMethod]
    public async Task Load_SchemaVersion3_MigratesRemovedLiquidGlassToMica()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        await File.WriteAllTextAsync(path, SchemaVersion3Document("LiquidGlassWallpaper"));
        using var repository = new TimerDataRepository(path);

        TimerWorkspace? loaded = await repository.LoadAsync();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(
            TimerBackgroundMode.Mica,
            loaded.Preferences.Background,
            "The removed Liquid Glass material migrates to Mica rather than corrupting the file.");
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

    [TestMethod]
    public async Task SaveAndLoad_RoundTripsTheLayoutModeAndTheClockAnimationFlag()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        using var repository = new TimerDataRepository(path);
        TimerWorkspace workspace = CreateWorkspace();
        workspace.UpdatePreferences(new TimerPreferences(
            TimerThemeMode.Dark,
            TimerBackgroundMode.Acrylic,
            TimerPreferences.EnglishLanguage,
            "Segoe UI",
            480,
            "2.0.0",
            TimerLayoutMode.SidePanel,
            clockAnimationEnabled: false));

        await repository.SaveAsync(workspace);
        TimerWorkspace? loaded = await repository.LoadAsync();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(TimerLayoutMode.SidePanel, loaded.Preferences.LayoutMode);
        Assert.IsFalse(loaded.Preferences.ClockAnimationEnabled);

        string json = await File.ReadAllTextAsync(path);
        StringAssert.Contains(json, "\"layoutMode\": \"SidePanel\"");
        StringAssert.Contains(json, "\"clockAnimationEnabled\": false");
    }

    [TestMethod]
    public async Task Load_DocumentWithoutTheNewPreferences_FallsBackToTheDefaults()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");

        // A document written before the layout / clock-animation preferences existed: both keys are simply absent.
        await File.WriteAllTextAsync(path, SchemaVersion3Document("Mica"));
        using var repository = new TimerDataRepository(path);

        TimerWorkspace? loaded = await repository.LoadAsync();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(TimerLayoutMode.Flyout, loaded.Preferences.LayoutMode);
        Assert.IsTrue(loaded.Preferences.ClockAnimationEnabled);
    }

    [TestMethod]
    public async Task Load_UnknownLayoutMode_FallsBackToFlyoutRatherThanFailingTheLoad()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        await File.WriteAllTextAsync(
            path,
            SchemaVersion3Document("Mica").Replace(
                "\"fontFamily\": \"Segoe UI\"",
                "\"fontFamily\": \"Segoe UI\",\n    \"layoutMode\": \"HolographicDrawer\"",
                StringComparison.Ordinal));
        using var repository = new TimerDataRepository(path);

        TimerWorkspace? loaded = await repository.LoadAsync();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(TimerLayoutMode.Flyout, loaded.Preferences.LayoutMode);
    }

    private static TimerWorkspace CreateWorkspace(TimerBackgroundMode background = TimerBackgroundMode.Acrylic)
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
            background,
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

    private static string SchemaVersion2Document(string background) =>
        $$"""
        {
          "schemaVersion": 2,
          "selectedFolderId": "11111111-1111-1111-1111-111111111111",
          "preferences": {
            "theme": "Dark",
            "background": "{{background}}",
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
        """;

    private static string SchemaVersion3Document(string background) =>
        SchemaVersion2Document(background).Replace(
            "\"schemaVersion\": 2",
            "\"schemaVersion\": 3",
            StringComparison.Ordinal);
}
