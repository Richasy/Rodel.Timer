using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Persistence;

internal static class TimerDataMapper
{
    public const int CurrentSchemaVersion = 1;

    public static TimerDataDocument ToDocument(TimerWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return new TimerDataDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            SelectedFolderId = workspace.SelectedFolderId,
            Preferences = new TimerPreferencesData
            {
                Theme = workspace.Preferences.Theme,
                Language = workspace.Preferences.Language,
                FontFamily = workspace.Preferences.FontFamily,
                DailyTargetMinutes = workspace.Preferences.DailyTargetMinutes,
                LastSeenVersion = workspace.Preferences.LastSeenVersion,
            },
            Folders =
            [
                .. workspace.Folders.Select(folder => new TimerFolderData
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    CreatedAt = folder.CreatedAt,
                    Icon = folder.Icon,
                }),
            ],
            Entries =
            [
                .. workspace.Entries.Select(entry => new TimerEntryData
                {
                    Id = entry.Id,
                    FolderId = entry.FolderId,
                    Title = entry.Title,
                    DurationSeconds = entry.DurationSeconds,
                    CreatedAt = entry.CreatedAt,
                }),
            ],
        };
    }

    public static TimerWorkspace FromDocument(TimerDataDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new UnsupportedTimerDataVersionException(
                document.SchemaVersion,
                CurrentSchemaVersion);
        }

        if (document.Preferences is null || document.Folders is null || document.Entries is null)
        {
            throw new InvalidDataException("The timer data document is missing a required section.");
        }

        if (document.Folders.Any(static folder => folder is null)
            || document.Entries.Any(static entry => entry is null))
        {
            throw new InvalidDataException("The timer data document contains a null item.");
        }

        TimerPreferences preferences = new(
            document.Preferences.Theme,
            document.Preferences.Language,
            document.Preferences.FontFamily,
            document.Preferences.DailyTargetMinutes,
            document.Preferences.LastSeenVersion);

        IEnumerable<TimerFolder> folders = document.Folders.Select(folder => new TimerFolder(
            folder!.Id,
            folder.Name,
            folder.CreatedAt,
            folder.Icon));

        IEnumerable<TimerEntry> entries = document.Entries.Select(entry => new TimerEntry(
            entry!.Id,
            entry.FolderId,
            entry.Title,
            entry.DurationSeconds,
            entry.CreatedAt));

        return TimerWorkspace.Restore(folders, entries, document.SelectedFolderId, preferences);
    }
}
