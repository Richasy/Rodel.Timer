using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of <see cref="TimerRowProjector"/> — the pure projection the folder/history list repeaters bind to. These
/// assert the same-key-rebind contract the panes rely on now that the whole-list <c>Keyed(ThemeStamp, …)</c> wrapper is
/// gone: a selection or theme/language change must produce a <b>new</b> row value at the <b>same</b> Guid key (so the
/// repeater rebinds the survivor in place and keeps projecting its interaction + accessibility nodes), while an
/// unrelated row's value is untouched.
/// </summary>
[TestClass]
public sealed class TimerRowProjectorTests
{
    private static readonly Guid FolderA = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FolderB = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FolderC = new("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset When = new(2026, 7, 14, 1, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<TimerFolder> ThreeFolders() =>
    [
        new TimerFolder(FolderA, "Focus", When, TimerFolderIcon.Book),
        new TimerFolder(FolderB, "Reading", When, TimerFolderIcon.Book),
        new TimerFolder(FolderC, "Work", When, TimerFolderIcon.Book),
    ];

    [TestMethod]
    public void FolderRows_FlagOnlyTheSelectedFolder()
    {
        IReadOnlyList<FolderRowData> rows = TimerRowProjector.FolderRows(ThreeFolders(), FolderB, revision: 0);

        Assert.HasCount(3, rows);
        Assert.IsFalse(rows[0].IsSelected);
        Assert.IsTrue(rows[1].IsSelected);
        Assert.IsFalse(rows[2].IsSelected);
    }

    [TestMethod]
    public void FolderRows_KeysMatchFolderIdsInOrderAndAreUnique()
    {
        IReadOnlyList<FolderRowData> rows = TimerRowProjector.FolderRows(ThreeFolders(), FolderA, revision: 0);

        Guid[] keys = [.. rows.Select(static r => r.Folder.Id)];
        CollectionAssert.AreEqual(new[] { FolderA, FolderB, FolderC }, keys);
        Assert.AreEqual(keys.Length, keys.Distinct().Count());
    }

    [TestMethod]
    public void FolderRows_StampTheRevisionOnEveryRow()
    {
        IReadOnlyList<FolderRowData> rows = TimerRowProjector.FolderRows(ThreeFolders(), FolderA, revision: 7);

        Assert.IsTrue(rows.All(static r => r.Revision == 7));
    }

    [TestMethod]
    public void FolderRows_ARevisionBump_ProducesNewValuesAtTheSameKeys()
    {
        IReadOnlyList<TimerFolder> folders = ThreeFolders();
        IReadOnlyList<FolderRowData> before = TimerRowProjector.FolderRows(folders, FolderA, revision: 1);
        IReadOnlyList<FolderRowData> after = TimerRowProjector.FolderRows(folders, FolderA, revision: 2);

        for (int i = 0; i < before.Count; i++)
        {
            // Same Guid key (the survivor), but a different value — this is exactly what drives the same-key rebind that
            // re-themes the row without a whole-list keyed remount.
            Assert.AreEqual(before[i].Folder.Id, after[i].Folder.Id);
            Assert.AreNotEqual(before[i], after[i]);
        }
    }

    [TestMethod]
    public void FolderRows_ASelectionMove_ChangesOnlyTheTwoAffectedRows()
    {
        IReadOnlyList<TimerFolder> folders = ThreeFolders();
        IReadOnlyList<FolderRowData> before = TimerRowProjector.FolderRows(folders, FolderA, revision: 0);
        IReadOnlyList<FolderRowData> after = TimerRowProjector.FolderRows(folders, FolderB, revision: 0);

        Assert.AreNotEqual(before[0], after[0]); // A loses selection
        Assert.AreNotEqual(before[1], after[1]); // B gains selection
        Assert.AreEqual(before[2], after[2]);    // C untouched
    }

    [TestMethod]
    public void HistoryRows_StampTheRevisionAndPreserveOrderAndKeys()
    {
        var entryA = new TimerEntry(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), FolderA, "A1", 60, When);
        var entryB = new TimerEntry(new Guid("bbbbbbbb-0000-0000-0000-000000000002"), FolderA, "B1", 120, When);

        IReadOnlyList<HistoryRowData> rows = TimerRowProjector.HistoryRows([entryA, entryB], revision: 3);

        Assert.HasCount(2, rows);
        Assert.AreEqual(entryA.Id, rows[0].Entry.Id);
        Assert.AreEqual(entryB.Id, rows[1].Entry.Id);
        Assert.IsTrue(rows.All(static r => r.Revision == 3));
    }

    [TestMethod]
    public void HistoryRows_ARevisionBump_ProducesNewValuesAtTheSameKeys()
    {
        var entry = new TimerEntry(new Guid("aaaaaaaa-0000-0000-0000-000000000001"), FolderA, "A1", 60, When);
        IReadOnlyList<HistoryRowData> before = TimerRowProjector.HistoryRows([entry], revision: 1);
        IReadOnlyList<HistoryRowData> after = TimerRowProjector.HistoryRows([entry], revision: 2);

        Assert.AreEqual(before[0].Entry.Id, after[0].Entry.Id);
        Assert.AreNotEqual(before[0], after[0]);
    }

    [TestMethod]
    public void FolderRows_NullFolders_Throws() =>
        Assert.ThrowsExactly<ArgumentNullException>(static () => TimerRowProjector.FolderRows(null!, Guid.Empty, 0));

    [TestMethod]
    public void HistoryRows_NullEntries_Throws() =>
        Assert.ThrowsExactly<ArgumentNullException>(static () => TimerRowProjector.HistoryRows(null!, 0));
}
