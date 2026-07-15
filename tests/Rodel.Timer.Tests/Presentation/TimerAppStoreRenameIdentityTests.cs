using Rodel.Timer.Core.Folders;
using Rodel.Timer.Tests.TestSupport;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// The "same-key rename identity" invariant the folder/history repeaters depend on: a mutation re-supplies a fresh
/// snapshot array reference (so a reconciling <c>ItemsRepeater</c> sees a change), yet each surviving entity keeps its
/// stable <see cref="Guid"/> key — so the repeater rebinds the affected row <em>in place</em> (updating its label /
/// icon) instead of remounting it, and unaffected rows and the selection are undisturbed.
/// </summary>
[TestClass]
public sealed class TimerAppStoreRenameIdentityTests
{
    [TestMethod]
    public void RenamingAFolder_ReSuppliesTheSnapshotButKeepsTheGuidKey()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        IReadOnlyList<TimerFolder> before = harness.Store.Folders;
        int revisionBefore = harness.Store.Revision;

        harness.Store.UpdateFolder(TimerStoreHarness.FolderAId, "Deep Work", TimerFolderIcon.Coffee);

        IReadOnlyList<TimerFolder> after = harness.Store.Folders;

        Assert.AreNotSame(before, after, "a mutation must hand back a fresh array so the repeater reconciles.");
        Assert.IsGreaterThan(revisionBefore, harness.Store.Revision, "a mutation bumps the revision.");
        Assert.HasCount(before.Count, after, "renaming changes no row count.");

        TimerFolder renamed = after.Single(folder => folder.Id == TimerStoreHarness.FolderAId);
        Assert.AreEqual("Deep Work", renamed.Name, "the same-key row carries the new name.");
        Assert.AreEqual(TimerFolderIcon.Coffee, renamed.Icon, "the same-key row carries the new icon.");
    }

    [TestMethod]
    public void RenamingAFolder_LeavesOtherRowsAndTheSelectionUnchanged()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        TimerFolder otherBefore = harness.Store.Folders.Single(folder => folder.Id == TimerStoreHarness.FolderBId);

        harness.Store.UpdateFolder(TimerStoreHarness.FolderAId, "Deep Work", TimerFolderIcon.Coffee);

        TimerFolder otherAfter = harness.Store.Folders.Single(folder => folder.Id == TimerStoreHarness.FolderBId);
        Assert.AreEqual(otherBefore.Name, otherAfter.Name, "an unrelated row keeps its label.");
        Assert.AreEqual(otherBefore.Icon, otherAfter.Icon, "an unrelated row keeps its icon.");
        Assert.AreEqual(TimerStoreHarness.FolderAId, harness.Store.SelectedFolder.Id, "the selection is undisturbed by a rename.");
    }

    [TestMethod]
    public void FolderKeySetIsStableAcrossARename()
    {
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();

        Guid[] keysBefore = harness.Store.Folders.Select(static folder => folder.Id).OrderBy(static id => id).ToArray();

        harness.Store.UpdateFolder(TimerStoreHarness.FolderAId, "Deep Work", TimerFolderIcon.Coffee);

        Guid[] keysAfter = harness.Store.Folders.Select(static folder => folder.Id).OrderBy(static id => id).ToArray();
        CollectionAssert.AreEqual(keysBefore, keysAfter, "a rename changes no repeater keys, so every row rebinds in place.");
    }
}
