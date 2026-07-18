using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;
using Rodel.Timer.Presentation;
using Rodel.Timer.Tests.TestSupport;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAppStoreMutationTests
{
    [TestMethod]
    public void EveryMutation_BumpsRevisionByExactlyOne()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        AssertBumpsRevisionByOne(harness, store => store.SelectFolder(TimerStoreHarness.FolderBId));
        AssertBumpsRevisionByOne(harness, store => store.AddFolder("Reading", TimerFolderIcon.Book));
        AssertBumpsRevisionByOne(harness, store => store.UpdateFolder(TimerStoreHarness.FolderAId, "Work!", TimerFolderIcon.Briefcase));
        AssertBumpsRevisionByOne(harness, store => store.UpdateTheme(TimerThemeMode.Dark));
        AssertBumpsRevisionByOne(harness, store => store.UpdateBackground(TimerBackgroundMode.AcrylicThin));
        AssertBumpsRevisionByOne(harness, store => store.UpdateLanguage("zh-CN"));
        AssertBumpsRevisionByOne(harness, store => store.UpdateFontFamily("Consolas"));
        AssertBumpsRevisionByOne(harness, store => store.UpdateDailyTarget(120));
        AssertBumpsRevisionByOne(harness, store => store.UpdateLastSeenVersion("1.2.3"));
    }

    [TestMethod]
    public void EveryMutation_ArmsThePersistenceDebounce()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        harness.Store.UpdateDailyTarget(90);

        Assert.IsTrue(harness.Store.SaveState.IsIdle, "A mutation only arms the debounce; the save has not run yet.");
        Assert.IsGreaterThan(0, harness.Scheduler.PendingCallbackCount);
    }

    [TestMethod]
    public void AddFolder_UsesTheInjectedIdentifierAndTimestamp()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Time.Advance(TimeSpan.FromHours(1));

        TimerFolder folder = harness.Store.AddFolder("Reading", TimerFolderIcon.Book);

        Assert.AreNotEqual(Guid.Empty, folder.Id);
        Assert.AreEqual("Reading", folder.Name);
        Assert.AreEqual(TimerFolderIcon.Book, folder.Icon);
        Assert.AreEqual(harness.Time.GetUtcNow(), folder.CreatedAt);
        Assert.IsTrue(harness.Store.Folders.Any(item => item.Id == folder.Id));
    }

    [TestMethod]
    public void AddFolder_EmptyName_Throws()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.Throws<ArgumentException>(() => harness.Store.AddFolder("   ", TimerFolderIcon.Book));
    }

    [TestMethod]
    public void UpdateFolder_PreservesIdentityAndCreationTime()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        TimerFolder original = harness.Store.Folders.First(folder => folder.Id == TimerStoreHarness.FolderAId);

        harness.Store.UpdateFolder(TimerStoreHarness.FolderAId, "Renamed", TimerFolderIcon.Home);
        TimerFolder updated = harness.Store.Folders.First(folder => folder.Id == TimerStoreHarness.FolderAId);

        Assert.AreEqual("Renamed", updated.Name);
        Assert.AreEqual(TimerFolderIcon.Home, updated.Icon);
        Assert.AreEqual(original.Id, updated.Id);
        Assert.AreEqual(original.CreatedAt, updated.CreatedAt);
    }

    [TestMethod]
    public void UpdateFolder_Unknown_Throws()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.Throws<KeyNotFoundException>(
            () => harness.Store.UpdateFolder(Guid.NewGuid(), "Nope", TimerFolderIcon.Home));
    }

    [TestMethod]
    public void RemoveFolder_ReturnsCascadeCount_AndDeletesEntries()
    {
        using var harness = new TimerStoreHarness();
        DateTimeOffset when = harness.Time.GetUtcNow();
        var entryB1 = new TimerEntry(new Guid("bbbbbbbb-0000-0000-0000-000000000001"), TimerStoreHarness.FolderBId, "B1", 60, when);
        var entryB2 = new TimerEntry(new Guid("bbbbbbbb-0000-0000-0000-000000000002"), TimerStoreHarness.FolderBId, "B2", 30, when);
        var entryA1 = new TimerEntry(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), TimerStoreHarness.FolderAId, "A1", 15, when);
        TimerWorkspace seeded = harness.BuildWorkspace([entryB1, entryB2, entryA1]);
        harness.LoadToReady(new TimerBootstrapResult(seeded, TimerBootstrapOrigin.Existing, []));

        int removed = harness.Store.RemoveFolder(TimerStoreHarness.FolderBId);

        Assert.AreEqual(2, removed);
        Assert.IsFalse(harness.Store.Folders.Any(folder => folder.Id == TimerStoreHarness.FolderBId));
        Assert.HasCount(1, harness.Store.Entries);
    }

    [TestMethod]
    public void RemoveFolder_LastFolder_Throws()
    {
        using var harness = new TimerStoreHarness();
        var only = new TimerFolder(TimerStoreHarness.FolderAId, "Only", harness.Time.GetUtcNow(), TimerFolderIcon.Briefcase);
        TimerWorkspace single = TimerWorkspace.Create(only);
        harness.LoadToReady(new TimerBootstrapResult(single, TimerBootstrapOrigin.Created, []));

        Assert.Throws<InvalidOperationException>(() => harness.Store.RemoveFolder(TimerStoreHarness.FolderAId));
    }

    [TestMethod]
    public void MoveEntry_ReassignsTheOwningFolder()
    {
        using var harness = new TimerStoreHarness();
        DateTimeOffset when = harness.Time.GetUtcNow();
        var entry = new TimerEntry(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), TimerStoreHarness.FolderAId, "A1", 60, when);
        TimerWorkspace seeded = harness.BuildWorkspace([entry]);
        harness.LoadToReady(new TimerBootstrapResult(seeded, TimerBootstrapOrigin.Existing, []));

        harness.Store.MoveEntry(entry.Id, TimerStoreHarness.FolderBId);

        TimerEntry moved = harness.Store.Entries.First(item => item.Id == entry.Id);
        Assert.AreEqual(TimerStoreHarness.FolderBId, moved.FolderId);
    }

    [TestMethod]
    public void RemoveEntry_DeletesIt()
    {
        using var harness = new TimerStoreHarness();
        DateTimeOffset when = harness.Time.GetUtcNow();
        var entry = new TimerEntry(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), TimerStoreHarness.FolderAId, "A1", 60, when);
        TimerWorkspace seeded = harness.BuildWorkspace([entry]);
        harness.LoadToReady(new TimerBootstrapResult(seeded, TimerBootstrapOrigin.Existing, []));

        harness.Store.RemoveEntry(entry.Id);

        Assert.IsEmpty(harness.Store.Entries);
    }

    [TestMethod]
    public void PreferenceMutations_PreserveEveryOtherField()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        harness.Store.UpdateLanguage("zh-CN");
        harness.Store.UpdateBackground(TimerBackgroundMode.AcrylicThin);
        harness.Store.UpdateFontFamily("Consolas");
        harness.Store.UpdateDailyTarget(120);
        harness.Store.UpdateLastSeenVersion("2.0.0");
        harness.Store.UpdateTheme(TimerThemeMode.Dark);

        TimerPreferences preferences = harness.Store.Preferences;
        Assert.AreEqual(TimerThemeMode.Dark, preferences.Theme);
        Assert.AreEqual(TimerBackgroundMode.AcrylicThin, preferences.Background);
        Assert.AreEqual("zh-CN", preferences.Language);
        Assert.AreEqual("Consolas", preferences.FontFamily);
        Assert.AreEqual(120, preferences.DailyTargetMinutes);
        Assert.AreEqual("2.0.0", preferences.LastSeenVersion);
    }

    [TestMethod]
    public void UpdateDailyTarget_OutOfRange_Throws()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Store.UpdateDailyTarget(0));
    }

    [TestMethod]
    public void NextId_Empty_Throws()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.IdFactory = () => Guid.Empty;

        Assert.Throws<InvalidOperationException>(() => harness.Store.AddFolder("Reading", TimerFolderIcon.Book));
    }

    [TestMethod]
    public void Mutation_BeforeLoad_Throws_NeverSilentlyReturns()
    {
        using var harness = new TimerStoreHarness();

        Assert.Throws<InvalidOperationException>(() => harness.Store.SelectFolder(TimerStoreHarness.FolderAId));
        Assert.Throws<InvalidOperationException>(() => harness.Store.UpdateDailyTarget(90));
    }

    [TestMethod]
    public void FailedMutation_DoesNotBumpRevisionOrArmSave()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        int revision = harness.Store.Revision;

        Assert.Throws<KeyNotFoundException>(() => harness.Store.SelectFolder(Guid.NewGuid()));

        Assert.AreEqual(revision, harness.Store.Revision);
        Assert.AreEqual(0, harness.Scheduler.PendingCallbackCount);
    }

    private static void AssertBumpsRevisionByOne(TimerStoreHarness harness, Action<TimerAppStore> mutation)
    {
        int before = harness.Store.Revision;
        mutation(harness.Store);
        Assert.AreEqual(before + 1, harness.Store.Revision);
    }
}
