using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;
using Rodel.Timer.Tests.TestSupport;

using Sprout.Reactive;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class TimerAppStoreSnapshotTests
{
    [TestMethod]
    public void Snapshots_SameRevision_AreReferenceEqual()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        IReadOnlyList<TimerFolder> folders1 = harness.Store.Folders;
        IReadOnlyList<TimerFolder> folders2 = harness.Store.Folders;
        IReadOnlyList<TimerEntry> entries1 = harness.Store.Entries;
        IReadOnlyList<TimerEntry> entries2 = harness.Store.Entries;
        IReadOnlyList<TimerEntry> selected1 = harness.Store.SelectedEntries;
        IReadOnlyList<TimerEntry> selected2 = harness.Store.SelectedEntries;

        Assert.AreSame(folders1, folders2);
        Assert.AreSame(entries1, entries2);
        Assert.AreSame(selected1, selected2);
    }

    [TestMethod]
    public void Snapshots_AfterMutation_AreNewReferencesWithNewContent()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        IReadOnlyList<TimerFolder> before = harness.Store.Folders;
        harness.Store.AddFolder("Reading", TimerFolderIcon.Book);
        IReadOnlyList<TimerFolder> after = harness.Store.Folders;

        Assert.AreNotSame(before, after);
        Assert.HasCount(before.Count + 1, after);
        Assert.IsTrue(after.Any(folder => folder.Name == "Reading"));
    }

    [TestMethod]
    public void ReadOnlyCollectionTrap_TheStoreDoesNotHandBackTheWorkspaceViews()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        TimerWorkspace workspace = harness.Store.Bootstrap.Value!.Workspace;

        // The workspace's own collection reference never changes across a mutation (it mutates in place); the store's
        // snapshot must therefore be a distinct object so a reconciling list actually sees a change.
        Assert.AreNotSame(workspace.Folders, harness.Store.Folders);
    }

    [TestMethod]
    public void SelectedEntries_FilterToTheSelectedFolder()
    {
        using var harness = new TimerStoreHarness();
        DateTimeOffset when = harness.Time.GetUtcNow();
        var entryA = new TimerEntry(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), TimerStoreHarness.FolderAId, "A1", 60, when);
        var entryB = new TimerEntry(new Guid("bbbbbbbb-0000-0000-0000-000000000001"), TimerStoreHarness.FolderBId, "B1", 120, when);
        TimerWorkspace seeded = harness.BuildWorkspace([entryA, entryB]);
        harness.LoadToReady(new TimerBootstrapResult(seeded, TimerBootstrapOrigin.Existing, []));

        Assert.HasCount(2, harness.Store.Entries);
        Assert.HasCount(1, harness.Store.SelectedEntries);
        Assert.AreEqual(entryA.Id, harness.Store.SelectedEntries[0].Id);

        harness.Store.SelectFolder(TimerStoreHarness.FolderBId);
        Assert.HasCount(1, harness.Store.SelectedEntries);
        Assert.AreEqual(entryB.Id, harness.Store.SelectedEntries[0].Id);
    }

    [TestMethod]
    public void SelectedFolder_TracksTheSelection()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.AreEqual(TimerStoreHarness.FolderAId, harness.Store.SelectedFolder.Id);

        harness.Store.SelectFolder(TimerStoreHarness.FolderBId);
        Assert.AreEqual(TimerStoreHarness.FolderBId, harness.Store.SelectedFolder.Id);
    }

    [TestMethod]
    public void Effect_SubscribedToFolders_RerunsOnMutation()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        int runs = 0;
        using var effect = new Effect(() =>
        {
            _ = harness.Store.Folders;
            runs++;
        });

        Assert.AreEqual(1, runs);

        harness.Store.AddFolder("Reading", TimerFolderIcon.Book);
        Assert.AreEqual(2, runs);

        harness.Store.SelectFolder(TimerStoreHarness.FolderBId);
        Assert.AreEqual(3, runs);
    }

    [TestMethod]
    public void Effect_SubscribedToBootstrap_RerunsWhenTheWorkspaceLoads()
    {
        using var harness = new TimerStoreHarness();

        int runs = 0;
        bool hasValue = false;
        using var effect = new Effect(() =>
        {
            hasValue = harness.Store.Bootstrap.HasValue;
            runs++;
        });

        Assert.AreEqual(1, runs);
        Assert.IsFalse(hasValue);

        harness.LoadToReady();

        Assert.IsGreaterThanOrEqualTo(2, runs);
        Assert.IsTrue(hasValue);
    }

    [TestMethod]
    public void Preferences_ReflectTheLoadedWorkspace()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Assert.AreEqual(TimerPreferences.Default.Language, harness.Store.Preferences.Language);
    }
}
