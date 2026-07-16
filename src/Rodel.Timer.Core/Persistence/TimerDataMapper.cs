using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Persistence;

internal static class TimerDataMapper
{
    public const int CurrentSchemaVersion = 3;
    private const int OldestSupportedSchemaVersion = 1;

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
                Background = workspace.Preferences.Background.ToString(),
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

        if (document.SchemaVersion is < OldestSupportedSchemaVersion or > CurrentSchemaVersion)
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

        TimerBackgroundMode background = MapBackground(document.SchemaVersion, document.Preferences.Background);
        TimerPreferences preferences = new(
            document.Preferences.Theme,
            background,
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

    private static TimerBackgroundMode MapBackground(int schemaVersion, string? persisted)
    {
        if (schemaVersion == 1)
        {
            return TimerBackgroundMode.Mica;
        }

        if (string.IsNullOrWhiteSpace(persisted))
        {
            throw new InvalidDataException($"Schema version {schemaVersion} preferences are missing the background mode.");
        }

        string value = persisted.Trim();
        if (schemaVersion == 2)
        {
            if (string.Equals(value, "SystemMaterial", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Solid", StringComparison.OrdinalIgnoreCase))
            {
                return TimerBackgroundMode.Mica;
            }

            if (string.Equals(value, "Acrylic", StringComparison.OrdinalIgnoreCase))
            {
                return TimerBackgroundMode.AcrylicThin;
            }

            if (string.Equals(value, "LiquidGlassWallpaper", StringComparison.OrdinalIgnoreCase))
            {
                return TimerBackgroundMode.LiquidGlassWallpaper;
            }

            throw new InvalidDataException($"Schema version 2 preferences contain unsupported background mode '{value}'.");
        }

        if (Enum.TryParse(value, ignoreCase: true, out TimerBackgroundMode background)
            && Enum.IsDefined(background)
            && string.Equals(value, background.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return background;
        }

        throw new InvalidDataException($"Schema version {schemaVersion} preferences contain unsupported background mode '{value}'.");
    }
}
