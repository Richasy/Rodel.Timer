using System.Collections.ObjectModel;

using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;

namespace Rodel.Timer.Core.Workspace;

public sealed class TimerWorkspace
{
    private readonly List<TimerFolder> _folders;
    private readonly ReadOnlyCollection<TimerFolder> _folderView;
    private readonly List<TimerEntry> _entries;
    private readonly ReadOnlyCollection<TimerEntry> _entryView;

    private TimerWorkspace(
        List<TimerFolder> folders,
        List<TimerEntry> entries,
        Guid selectedFolderId,
        TimerPreferences preferences)
    {
        _folders = folders;
        _folderView = folders.AsReadOnly();
        _entries = entries;
        _entryView = entries.AsReadOnly();
        SelectedFolderId = selectedFolderId;
        Preferences = preferences;
    }

    public IReadOnlyList<TimerFolder> Folders => _folderView;

    public IReadOnlyList<TimerEntry> Entries => _entryView;

    public Guid SelectedFolderId { get; private set; }

    public TimerPreferences Preferences { get; private set; }

    public static TimerWorkspace Create(
        TimerFolder initialFolder,
        TimerPreferences? preferences = null)
    {
        ArgumentNullException.ThrowIfNull(initialFolder);
        return new TimerWorkspace(
            [initialFolder],
            [],
            initialFolder.Id,
            preferences ?? TimerPreferences.Default);
    }

    public static TimerWorkspace Restore(
        IEnumerable<TimerFolder> folders,
        IEnumerable<TimerEntry> entries,
        Guid selectedFolderId,
        TimerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(preferences);

        List<TimerFolder> folderList = [.. folders];
        List<TimerEntry> entryList = [.. entries];
        Validate(folderList, entryList, selectedFolderId);

        return new TimerWorkspace(folderList, entryList, selectedFolderId, preferences);
    }

    public void SelectFolder(Guid folderId)
    {
        EnsureFolderExists(folderId);
        SelectedFolderId = folderId;
    }

    public void AddFolder(TimerFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        if (_folders.Any(item => item.Id == folder.Id))
        {
            throw new InvalidOperationException($"Folder '{folder.Id}' already exists.");
        }

        _folders.Add(folder);
    }

    public void UpdateFolder(TimerFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        int index = FindFolderIndex(folder.Id);
        _folders[index] = folder;
    }

    public int RemoveFolder(Guid folderId)
    {
        if (_folders.Count == 1)
        {
            throw new InvalidOperationException("The last folder cannot be removed.");
        }

        int index = FindFolderIndex(folderId);
        _folders.RemoveAt(index);
        int removedEntryCount = _entries.RemoveAll(entry => entry.FolderId == folderId);

        if (SelectedFolderId == folderId)
        {
            SelectedFolderId = _folders[0].Id;
        }

        return removedEntryCount;
    }

    public void AddEntry(TimerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        EnsureFolderExists(entry.FolderId);

        if (_entries.Any(item => item.Id == entry.Id))
        {
            throw new InvalidOperationException($"History entry '{entry.Id}' already exists.");
        }

        _entries.Add(entry);
    }

    public void RemoveEntry(Guid entryId)
    {
        int index = FindEntryIndex(entryId);
        _entries.RemoveAt(index);
    }

    public void MoveEntry(Guid entryId, Guid folderId)
    {
        EnsureFolderExists(folderId);
        int index = FindEntryIndex(entryId);
        TimerEntry entry = _entries[index];

        if (entry.FolderId != folderId)
        {
            _entries[index] = entry.MoveTo(folderId);
        }
    }

    public IReadOnlyList<TimerEntry> GetEntries(Guid folderId)
    {
        EnsureFolderExists(folderId);
        return _entries.Where(entry => entry.FolderId == folderId).ToArray();
    }

    public TimeSpan GetTotalDuration(DateOnly date, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        long totalSeconds = 0;
        foreach (TimerEntry entry in _entries)
        {
            DateTimeOffset localTime = TimeZoneInfo.ConvertTime(entry.CreatedAt, timeZone);
            if (DateOnly.FromDateTime(localTime.DateTime) == date)
            {
                totalSeconds = checked(totalSeconds + entry.DurationSeconds);
            }
        }

        return TimeSpan.FromSeconds(totalSeconds);
    }

    public void UpdatePreferences(TimerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        Preferences = preferences;
    }

    private static void Validate(
        List<TimerFolder> folders,
        List<TimerEntry> entries,
        Guid selectedFolderId)
    {
        if (folders.Count == 0)
        {
            throw new ArgumentException("A workspace must contain at least one folder.", nameof(folders));
        }

        if (folders.Any(folder => folder is null))
        {
            throw new ArgumentException("A workspace cannot contain a null folder.", nameof(folders));
        }

        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException("A workspace cannot contain a null history entry.", nameof(entries));
        }

        HashSet<Guid> folderIds = [];
        foreach (TimerFolder folder in folders)
        {
            if (!folderIds.Add(folder.Id))
            {
                throw new ArgumentException($"Folder id '{folder.Id}' is duplicated.", nameof(folders));
            }
        }

        if (!folderIds.Contains(selectedFolderId))
        {
            throw new ArgumentException("The selected folder does not exist.", nameof(selectedFolderId));
        }

        HashSet<Guid> entryIds = [];
        foreach (TimerEntry entry in entries)
        {
            if (!entryIds.Add(entry.Id))
            {
                throw new ArgumentException($"History entry id '{entry.Id}' is duplicated.", nameof(entries));
            }

            if (!folderIds.Contains(entry.FolderId))
            {
                throw new ArgumentException(
                    $"History entry '{entry.Id}' refers to missing folder '{entry.FolderId}'.",
                    nameof(entries));
            }
        }
    }

    private int FindFolderIndex(Guid folderId)
    {
        int index = _folders.FindIndex(folder => folder.Id == folderId);
        return index >= 0
            ? index
            : throw new KeyNotFoundException($"Folder '{folderId}' does not exist.");
    }

    private int FindEntryIndex(Guid entryId)
    {
        int index = _entries.FindIndex(entry => entry.Id == entryId);
        return index >= 0
            ? index
            : throw new KeyNotFoundException($"History entry '{entryId}' does not exist.");
    }

    private void EnsureFolderExists(Guid folderId) => _ = FindFolderIndex(folderId);
}
