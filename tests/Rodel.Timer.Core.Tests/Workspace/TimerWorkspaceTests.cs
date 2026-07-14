using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Tests.Workspace;

[TestClass]
public sealed class TimerWorkspaceTests
{
    [TestMethod]
    public void RemoveFolder_CascadesHistoryAndSelectsTheFirstRemainingFolder()
    {
        TimerFolder first = Folder("11111111111111111111111111111111", "First");
        TimerFolder second = Folder("22222222222222222222222222222222", "Second");
        TimerWorkspace workspace = TimerWorkspace.Create(first);
        workspace.AddFolder(second);
        workspace.SelectFolder(second.Id);
        workspace.AddEntry(Entry("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", second.Id, 30));
        workspace.AddEntry(Entry("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", first.Id, 60));

        int removed = workspace.RemoveFolder(second.Id);

        Assert.AreEqual(1, removed);
        Assert.AreEqual(first.Id, workspace.SelectedFolderId);
        Assert.HasCount(1, workspace.Folders);
        Assert.HasCount(1, workspace.Entries);
        Assert.AreEqual(first.Id, workspace.Entries[0].FolderId);
    }

    [TestMethod]
    public void RemoveFolder_RejectsTheLastFolderWithoutMutatingState()
    {
        TimerFolder folder = Folder("11111111111111111111111111111111", "Only");
        TimerWorkspace workspace = TimerWorkspace.Create(folder);

        Assert.Throws<InvalidOperationException>(
            () => workspace.RemoveFolder(folder.Id));
        Assert.HasCount(1, workspace.Folders);
        Assert.AreEqual(folder.Id, workspace.SelectedFolderId);
    }

    [TestMethod]
    public void MoveEntry_MovesToTheTargetFolderAndPreservesEntryIdentity()
    {
        TimerFolder first = Folder("11111111111111111111111111111111", "First");
        TimerFolder second = Folder("22222222222222222222222222222222", "Second");
        TimerWorkspace workspace = TimerWorkspace.Create(first);
        workspace.AddFolder(second);
        TimerEntry entry = Entry("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", first.Id, 30);
        workspace.AddEntry(entry);

        workspace.MoveEntry(entry.Id, second.Id);

        Assert.AreEqual(entry.Id, workspace.Entries[0].Id);
        Assert.AreEqual(second.Id, workspace.Entries[0].FolderId);
    }

    [TestMethod]
    public void GetTotalDuration_UsesTheRequestedLocalCalendarDate()
    {
        TimerFolder folder = Folder("11111111111111111111111111111111", "Work");
        TimerWorkspace workspace = TimerWorkspace.Create(folder);
        TimeZoneInfo timeZone = TimeZoneInfo.CreateCustomTimeZone(
            "UTC+08",
            TimeSpan.FromHours(8),
            "UTC+08",
            "UTC+08");
        workspace.AddEntry(new TimerEntry(
            Guid.NewGuid(),
            folder.Id,
            "Before midnight UTC",
            60,
            new DateTimeOffset(2026, 7, 13, 23, 30, 0, TimeSpan.Zero)));
        workspace.AddEntry(new TimerEntry(
            Guid.NewGuid(),
            folder.Id,
            "Previous local day",
            120,
            new DateTimeOffset(2026, 7, 13, 15, 30, 0, TimeSpan.Zero)));

        TimeSpan total = workspace.GetTotalDuration(new DateOnly(2026, 7, 14), timeZone);

        Assert.AreEqual(TimeSpan.FromMinutes(1), total);
    }

    [TestMethod]
    public void Restore_RejectsAnEntryWhoseFolderIsMissing()
    {
        TimerFolder folder = Folder("11111111111111111111111111111111", "Work");
        TimerEntry orphan = Entry(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            Guid.ParseExact("22222222222222222222222222222222", "N"),
            30);

        Assert.Throws<ArgumentException>(
            () => TimerWorkspace.Restore(
                [folder],
                [orphan],
                folder.Id,
                TimerPreferences.Default));
    }

    private static TimerFolder Folder(string id, string name) => new(
        Guid.ParseExact(id, "N"),
        name,
        DateTimeOffset.UnixEpoch,
        TimerFolderIcon.Activity);

    private static TimerEntry Entry(string id, Guid folderId, long durationSeconds) => new(
        Guid.ParseExact(id, "N"),
        folderId,
        "Task",
        durationSeconds,
        DateTimeOffset.UnixEpoch);
}
