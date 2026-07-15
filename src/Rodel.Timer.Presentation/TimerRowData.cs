using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;

namespace Rodel.Timer.Presentation;

/// <summary>
/// One folder row's projection input: the folder, whether it is the selected folder, and a monotonic theme/language
/// revision. It is the value the folder list's <c>ItemsRepeater</c> is keyed by (via <see cref="TimerFolder.Id"/>), so a
/// change to any field — a rename, a selection move, or a theme/language switch (which bumps <see cref="Revision"/>) —
/// makes the row's value differ and rebinds just that survivor in place, keeping its widget, scroll position and focus.
/// This is what lets the list re-theme and re-highlight <b>without</b> a whole-list keyed remount (which would tear the
/// nested repeater's interaction and accessibility trees out of the shell).
/// </summary>
/// <param name="Folder">The folder to render.</param>
/// <param name="IsSelected">Whether this folder is the currently selected folder.</param>
/// <param name="Revision">A monotonic theme/language stamp, so a theme or language switch changes the row value and rebinds it.</param>
public readonly record struct FolderRowData(
    TimerFolder Folder,
    bool IsSelected,
    int Revision);

/// <summary>
/// One history row's projection input: the entry plus a monotonic theme/language revision. It is the value the history
/// list's <c>ItemsRepeater</c> is keyed by (via <see cref="TimerEntry.Id"/>), so an edit or a theme/language switch
/// (which bumps <see cref="Revision"/>) rebinds just that survivor in place — the same same-key rebind the folder list
/// uses — without a whole-list keyed remount.
/// </summary>
/// <param name="Entry">The history entry to render.</param>
/// <param name="Revision">A monotonic theme/language stamp, so a theme or language switch changes the row value and rebinds it.</param>
public readonly record struct HistoryRowData(
    TimerEntry Entry,
    int Revision);

/// <summary>
/// Pure projections from the loaded folder/history state to the per-row data the list repeaters bind to. Kept separate
/// from the views so the projection (including the per-row selection flag and the theme revision that drives same-key
/// re-theming) is testable without a window.
/// </summary>
public static class TimerRowProjector
{
    /// <summary>Projects the folders to per-row data, flagging the selected folder and stamping the theme revision.</summary>
    /// <param name="folders">The folders to project.</param>
    /// <param name="selectedFolderId">The currently selected folder's id.</param>
    /// <param name="revision">The current theme/language revision.</param>
    /// <returns>One <see cref="FolderRowData"/> per folder, in order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="folders"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<FolderRowData> FolderRows(
        IReadOnlyList<TimerFolder> folders,
        Guid selectedFolderId,
        int revision)
    {
        ArgumentNullException.ThrowIfNull(folders);

        var rows = new FolderRowData[folders.Count];
        for (int i = 0; i < folders.Count; i++)
        {
            TimerFolder folder = folders[i];
            rows[i] = new FolderRowData(folder, folder.Id == selectedFolderId, revision);
        }

        return rows;
    }

    /// <summary>Projects the selected folder's entries to per-row data, stamping the theme revision.</summary>
    /// <param name="entries">The history entries to project.</param>
    /// <param name="revision">The current theme/language revision.</param>
    /// <returns>One <see cref="HistoryRowData"/> per entry, in order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<HistoryRowData> HistoryRows(
        IReadOnlyList<TimerEntry> entries,
        int revision)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var rows = new HistoryRowData[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            rows[i] = new HistoryRowData(entries[i], revision);
        }

        return rows;
    }
}
