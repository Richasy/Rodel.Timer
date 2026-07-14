using System.Collections.ObjectModel;

using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;

namespace Rodel.Timer.Core.Workspace;

/// <summary>
/// The mutable aggregate root for one user's timer data: its folders, history entries, selected folder,
/// and preferences. A workspace enforces its invariants on every mutation so it is never observed in an
/// invalid state: at least one folder always exists (the last folder cannot be removed), folder and entry
/// ids are unique, every entry's <see cref="History.TimerEntry.FolderId"/> refers to an existing folder
/// (removing a folder cascades to delete its entries), and <see cref="SelectedFolderId"/> always refers to
/// an existing folder (falling back to the first remaining folder if the selected one is removed).
/// A workspace is created fresh via <see cref="Create"/> or rebuilt from already-valid data via
/// <see cref="Restore"/>; there is no public parameterless or partially-constructed state.
/// </summary>
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

    /// <summary>The workspace's folders, in their current order. Always contains at least one folder.</summary>
    public IReadOnlyList<TimerFolder> Folders => _folderView;

    /// <summary>The workspace's history entries. Every entry's folder is guaranteed to exist in <see cref="Folders"/>.</summary>
    public IReadOnlyList<TimerEntry> Entries => _entryView;

    /// <summary>The identifier of the currently selected folder. Always refers to an existing entry in <see cref="Folders"/>.</summary>
    public Guid SelectedFolderId { get; private set; }

    /// <summary>The workspace's current preferences.</summary>
    public TimerPreferences Preferences { get; private set; }

    /// <summary>
    /// Creates a brand-new workspace containing only <paramref name="initialFolder"/>, selected by default,
    /// with no history entries.
    /// </summary>
    /// <param name="initialFolder">The workspace's first (and initially only) folder.</param>
    /// <param name="preferences">The initial preferences, or <see langword="null"/> to use <see cref="TimerPreferences.Default"/>.</param>
    /// <returns>A new, valid <see cref="TimerWorkspace"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initialFolder"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Rebuilds a workspace from previously validated data (loaded from a repository or migrated from
    /// legacy data), re-validating every invariant before returning.
    /// </summary>
    /// <param name="folders">The folders to restore. Must be non-empty, contain no null entries, and have unique ids.</param>
    /// <param name="entries">The history entries to restore. Must contain no null entries, must have unique ids, and each must reference a folder present in <paramref name="folders"/>.</param>
    /// <param name="selectedFolderId">The folder to select. Must reference a folder present in <paramref name="folders"/>.</param>
    /// <param name="preferences">The preferences to restore.</param>
    /// <returns>A restored, valid <see cref="TimerWorkspace"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="folders"/>, <paramref name="entries"/>, or <paramref name="preferences"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Any workspace invariant listed above is violated.</exception>
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

    /// <summary>Selects <paramref name="folderId"/> as the current folder.</summary>
    /// <param name="folderId">The folder to select.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="folderId"/> does not exist in <see cref="Folders"/>.</exception>
    public void SelectFolder(Guid folderId)
    {
        EnsureFolderExists(folderId);
        SelectedFolderId = folderId;
    }

    /// <summary>Adds <paramref name="folder"/> to the workspace.</summary>
    /// <param name="folder">The folder to add. Its id must not already exist in <see cref="Folders"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="folder"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A folder with the same id already exists.</exception>
    public void AddFolder(TimerFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        if (_folders.Any(item => item.Id == folder.Id))
        {
            throw new InvalidOperationException($"Folder '{folder.Id}' already exists.");
        }

        _folders.Add(folder);
    }

    /// <summary>Replaces the existing folder with the same id as <paramref name="folder"/>.</summary>
    /// <param name="folder">The updated folder. A folder with this id must already exist.</param>
    /// <exception cref="ArgumentNullException"><paramref name="folder"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">No folder with <paramref name="folder"/>'s id exists.</exception>
    public void UpdateFolder(TimerFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        int index = FindFolderIndex(folder.Id);
        _folders[index] = folder;
    }

    /// <summary>
    /// Removes the folder identified by <paramref name="folderId"/> and cascade-deletes every history entry
    /// that belonged to it. If the removed folder was selected, the first remaining folder becomes selected.
    /// </summary>
    /// <param name="folderId">The folder to remove.</param>
    /// <returns>The number of history entries removed along with the folder.</returns>
    /// <exception cref="InvalidOperationException">The workspace has only one folder; the last folder cannot be removed.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="folderId"/> does not exist in <see cref="Folders"/>.</exception>
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

    /// <summary>Adds <paramref name="entry"/> to the workspace.</summary>
    /// <param name="entry">The entry to add. Its <see cref="TimerEntry.FolderId"/> must reference an existing folder, and its id must not already exist in <see cref="Entries"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="entry"/>'s folder does not exist.</exception>
    /// <exception cref="InvalidOperationException">An entry with the same id already exists.</exception>
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

    /// <summary>Removes the history entry identified by <paramref name="entryId"/>.</summary>
    /// <param name="entryId">The entry to remove.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="entryId"/> does not exist in <see cref="Entries"/>.</exception>
    public void RemoveEntry(Guid entryId)
    {
        int index = FindEntryIndex(entryId);
        _entries.RemoveAt(index);
    }

    /// <summary>Moves the history entry identified by <paramref name="entryId"/> to <paramref name="folderId"/>.</summary>
    /// <param name="entryId">The entry to move.</param>
    /// <param name="folderId">The destination folder. Must already exist.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="folderId"/> or <paramref name="entryId"/> does not exist.</exception>
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

    /// <summary>Returns every history entry belonging to <paramref name="folderId"/>.</summary>
    /// <param name="folderId">The folder whose entries to return. Must already exist.</param>
    /// <returns>The entries belonging to the folder, in no particular order.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="folderId"/> does not exist.</exception>
    public IReadOnlyList<TimerEntry> GetEntries(Guid folderId)
    {
        EnsureFolderExists(folderId);
        return _entries.Where(entry => entry.FolderId == folderId).ToArray();
    }

    /// <summary>
    /// Computes the total duration of every entry across all folders whose <see cref="TimerEntry.CreatedAt"/>,
    /// converted to <paramref name="timeZone"/>, falls on <paramref name="date"/>. Because entries store an
    /// absolute <see cref="DateTimeOffset"/>, the calendar date a given entry counts toward depends on the
    /// caller-supplied time zone — the same instant can fall on different local dates in different zones.
    /// </summary>
    /// <param name="date">The local calendar date to total.</param>
    /// <param name="timeZone">The time zone used to convert each entry's timestamp to a local date.</param>
    /// <returns>The summed duration of all matching entries.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> is <see langword="null"/>.</exception>
    /// <exception cref="OverflowException">The summed duration overflows <see cref="long"/> seconds.</exception>
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

    /// <summary>Replaces <see cref="Preferences"/> with <paramref name="preferences"/>.</summary>
    /// <param name="preferences">The new preferences.</param>
    /// <exception cref="ArgumentNullException"><paramref name="preferences"/> is <see langword="null"/>.</exception>
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
