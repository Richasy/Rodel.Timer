using System.Globalization;
using System.Text.Json;

using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Migration;

/// <summary>
/// Converts a <see cref="LegacyWorkTimerSnapshot"/> from the previous Work-Timer application into a new
/// <see cref="TimerWorkspace"/>. Recoverable per-item issues (a missing name, an unknown icon codepoint, an
/// out-of-range preference, an entry referring to a missing folder, and similar) are repaired in place and
/// reported as warnings rather than aborting the import; only data that cannot be repaired into a valid
/// workspace (invalid JSON, a null item, a duplicate/empty-folder-set invariant violation) throws
/// <see cref="LegacyDataMigrationException"/>.
/// </summary>
public sealed class LegacyWorkTimerImporter
{
    private readonly string _defaultFolderName;
    private readonly TimerPreferences _defaultPreferences;
    private readonly TimeZoneInfo _legacyTimeZone;

    /// <summary>Creates an importer configured with the defaults and legacy time zone needed to repair recoverable issues.</summary>
    /// <param name="defaultFolderName">
    /// The base name used for a synthesized fallback folder (when the legacy data has no folders) and for
    /// unnamed legacy folders (suffixed with their 1-based index).
    /// </param>
    /// <param name="defaultPreferences">The preferences substituted for any legacy preference that is missing or invalid.</param>
    /// <param name="legacyTimeZone">
    /// The time zone used to interpret a legacy timestamp whose <see cref="DateTimeKind"/> is
    /// <see cref="DateTimeKind.Unspecified"/>, since the legacy application did not record an offset.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="defaultPreferences"/> or <paramref name="legacyTimeZone"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="defaultFolderName"/> is null, empty, or whitespace-only.</exception>
    public LegacyWorkTimerImporter(
        string defaultFolderName,
        TimerPreferences defaultPreferences,
        TimeZoneInfo legacyTimeZone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultFolderName);
        ArgumentNullException.ThrowIfNull(defaultPreferences);
        ArgumentNullException.ThrowIfNull(legacyTimeZone);

        _defaultFolderName = defaultFolderName.Trim();
        _defaultPreferences = defaultPreferences;
        _legacyTimeZone = legacyTimeZone;
    }

    /// <summary>
    /// Converts <paramref name="snapshot"/> into a new <see cref="TimerWorkspace"/>, repairing recoverable
    /// issues (missing names, unknown icons, out-of-range preferences, orphaned entries, an invalid selected
    /// folder) and recording them as warnings.
    /// </summary>
    /// <param name="snapshot">The legacy data to convert.</param>
    /// <param name="fallbackFolderId">
    /// The identifier used for a synthesized folder when <paramref name="snapshot"/> has no folders at all.
    /// Must not be <see cref="Guid.Empty"/>.
    /// </param>
    /// <param name="importedAt">
    /// The timestamp used for any folder or entry whose legacy creation time is missing, and as the
    /// reference point for interpreting legacy timestamps.
    /// </param>
    /// <returns>The migrated workspace together with any repair warnings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fallbackFolderId"/> is <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="LegacyDataMigrationException">
    /// The legacy JSON is invalid, contains a null item, or converts to data that violates a
    /// <see cref="TimerWorkspace"/> invariant.
    /// </exception>
    public LegacyImportResult Import(
        LegacyWorkTimerSnapshot snapshot,
        Guid fallbackFolderId,
        DateTimeOffset importedAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (fallbackFolderId == Guid.Empty)
        {
            throw new ArgumentException("A fallback folder id cannot be empty.", nameof(fallbackFolderId));
        }

        List<string> warnings = [];
        List<LegacyFolderData> legacyFolders = DeserializeFolders(snapshot.FolderListJson);
        List<LegacyHistoryData> legacyEntries = DeserializeEntries(snapshot.HistoryListJson);

        List<TimerFolder> folders = ConvertFolders(
            legacyFolders,
            fallbackFolderId,
            importedAt,
            warnings);
        List<TimerEntry> entries = ConvertEntries(legacyEntries, importedAt, warnings);
        RehomeOrphanedEntries(entries, folders, warnings);

        Guid selectedFolderId = ReadSelectedFolderId(snapshot.Settings, folders, warnings);
        TimerPreferences preferences = ReadPreferences(snapshot.Settings, warnings);

        try
        {
            TimerWorkspace workspace = TimerWorkspace.Restore(
                folders,
                entries,
                selectedFolderId,
                preferences);
            return new LegacyImportResult(workspace, warnings.AsReadOnly());
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new LegacyDataMigrationException(
                "The legacy Work-Timer data violates a workspace invariant.",
                exception);
        }
    }

    private static List<LegacyFolderData> DeserializeFolders(string? json) =>
        DeserializeList(
            json,
            LegacyWorkTimerJsonContext.Default.ListLegacyFolderData,
            "folder");

    private static List<LegacyHistoryData> DeserializeEntries(string? json) =>
        DeserializeList(
            json,
            LegacyWorkTimerJsonContext.Default.ListLegacyHistoryData,
            "history");

    private static List<T> DeserializeList<T>(
        string? json,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<List<T>> typeInfo,
        string description)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize(json, typeInfo)
                ?? throw new JsonException($"The legacy {description} list is null.");
        }
        catch (JsonException exception)
        {
            throw new LegacyDataMigrationException(
                $"The legacy {description} list is not valid JSON.",
                exception);
        }
    }

    private List<TimerFolder> ConvertFolders(
        List<LegacyFolderData> source,
        Guid fallbackFolderId,
        DateTimeOffset importedAt,
        List<string> warnings)
    {
        if (source.Count == 0)
        {
            return
            [
                new TimerFolder(
                    fallbackFolderId,
                    _defaultFolderName,
                    importedAt,
                    TimerFolderIcon.Activity),
            ];
        }

        List<TimerFolder> folders = new(source.Count);
        int index = 0;

        foreach (LegacyFolderData item in source)
        {
            if (item is null)
            {
                throw new LegacyDataMigrationException(
                    $"Legacy folder at index {index} is null.");
            }

            string name = item.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"{_defaultFolderName} {index + 1}";
                warnings.Add(
                    $"Legacy folder at index {index} had no name and was renamed to '{name}'.");
            }

            try
            {
                folders.Add(new TimerFolder(
                    ParseId(item.Id, "folder", index),
                    name,
                    ConvertLegacyTime(
                        item.CreateTime,
                        "folder",
                        index,
                        importedAt,
                        warnings),
                    LegacyIconMap.Resolve(item.Icon, warnings)));
            }
            catch (ArgumentException exception)
            {
                throw new LegacyDataMigrationException(
                    $"Legacy folder at index {index} is invalid.",
                    exception);
            }

            index++;
        }

        return folders;
    }

    private List<TimerEntry> ConvertEntries(
        List<LegacyHistoryData> source,
        DateTimeOffset importedAt,
        List<string> warnings)
    {
        List<TimerEntry> entries = new(source.Count);
        int index = 0;

        foreach (LegacyHistoryData item in source)
        {
            if (item is null)
            {
                throw new LegacyDataMigrationException(
                    $"Legacy history entry at index {index} is null.");
            }

            string title = item.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Untitled";
                warnings.Add(
                    $"Legacy history entry at index {index} had no title and was renamed to 'Untitled'.");
            }

            long durationSeconds = item.TotalSeconds;
            if (durationSeconds < 0)
            {
                warnings.Add(
                    $"Legacy history entry at index {index} had a negative duration and was clamped to zero.");
                durationSeconds = 0;
            }

            try
            {
                entries.Add(new TimerEntry(
                    ParseId(item.Id, "history entry", index),
                    ParseId(item.FolderId, "history folder", index),
                    title,
                    durationSeconds,
                    ConvertLegacyTime(
                        item.CreateTime,
                        "history entry",
                        index,
                        importedAt,
                        warnings)));
            }
            catch (ArgumentException exception)
            {
                throw new LegacyDataMigrationException(
                    $"Legacy history entry at index {index} is invalid.",
                    exception);
            }

            index++;
        }

        return entries;
    }

    private TimerPreferences ReadPreferences(
        IReadOnlyDictionary<string, string> settings,
        ICollection<string> warnings)
    {
        TimerThemeMode theme = ReadTheme(GetSetting(settings, "Theme"), warnings);
        string language = ReadLanguage(GetSetting(settings, "Language"), warnings);
        string fontFamily = GetSetting(settings, "FontFamily") ?? _defaultPreferences.FontFamily;
        int dailyTargetMinutes = ReadDailyTarget(
            GetSetting(settings, "PresetDuration"),
            warnings);
        string? lastSeenVersion = GetSetting(settings, "AppVersion");

        return new TimerPreferences(
            theme,
            language,
            string.IsNullOrWhiteSpace(fontFamily) ? _defaultPreferences.FontFamily : fontFamily,
            dailyTargetMinutes,
            lastSeenVersion);
    }

    private static void RehomeOrphanedEntries(
        List<TimerEntry> entries,
        List<TimerFolder> folders,
        List<string> warnings)
    {
        HashSet<Guid> folderIds = folders.Select(folder => folder.Id).ToHashSet();
        Guid fallbackFolderId = folders[0].Id;

        for (int index = 0; index < entries.Count; index++)
        {
            TimerEntry entry = entries[index];
            if (folderIds.Contains(entry.FolderId))
            {
                continue;
            }

            entries[index] = entry.MoveTo(fallbackFolderId);
            warnings.Add(
                $"Legacy history entry at index {index} referred to a missing folder and was moved to '{folders[0].Name}'.");
        }
    }

    private static Guid ReadSelectedFolderId(
        IReadOnlyDictionary<string, string> settings,
        List<TimerFolder> folders,
        List<string> warnings)
    {
        string? value = GetSetting(settings, "LastSelectFolderId");
        if (Guid.TryParse(value, out Guid selectedFolderId)
            && folders.Any(folder => folder.Id == selectedFolderId))
        {
            return selectedFolderId;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            warnings.Add("The legacy selected folder no longer exists; the first folder was selected.");
        }

        return folders[0].Id;
    }

    private TimerThemeMode ReadTheme(string? value, ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return _defaultPreferences.Theme;
        }

        if (Enum.TryParse(value, ignoreCase: true, out TimerThemeMode theme))
        {
            return theme;
        }

        warnings.Add($"Legacy theme '{value}' is unsupported and was reset to System.");
        return TimerThemeMode.System;
    }

    private string ReadLanguage(string? value, ICollection<string> warnings)
    {
        if (string.Equals(value, "zh_CN", StringComparison.OrdinalIgnoreCase))
        {
            return TimerPreferences.ChineseLanguage;
        }

        if (string.Equals(value, "en_US", StringComparison.OrdinalIgnoreCase))
        {
            return TimerPreferences.EnglishLanguage;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return _defaultPreferences.Language;
        }

        warnings.Add(
            $"Legacy language '{value}' is unsupported and was reset to {_defaultPreferences.Language}.");
        return _defaultPreferences.Language;
    }

    private int ReadDailyTarget(string? value, ICollection<string> warnings)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes)
            && minutes is >= 1 and <= TimerPreferences.MaximumDailyTargetMinutes)
        {
            return minutes;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            warnings.Add(
                $"Legacy daily target '{value}' is invalid and was reset to {_defaultPreferences.DailyTargetMinutes} minutes.");
        }

        return _defaultPreferences.DailyTargetMinutes;
    }

    private DateTimeOffset ConvertLegacyTime(
        DateTime value,
        string description,
        int index,
        DateTimeOffset fallback,
        List<string> warnings)
    {
        if (value == default)
        {
            warnings.Add(
                $"Legacy {description} at index {index} had no creation time and was dated at migration.");
            return fallback;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value),
            DateTimeKind.Local => new DateTimeOffset(value),
            _ => new DateTimeOffset(value, _legacyTimeZone.GetUtcOffset(value)),
        };
    }

    private static Guid ParseId(string? value, string description, int index)
    {
        if (Guid.TryParse(value, out Guid id) && id != Guid.Empty)
        {
            return id;
        }

        throw new ArgumentException(
            $"Legacy {description} at index {index} has invalid id '{value}'.",
            nameof(value));
    }

    private static string? GetSetting(
        IReadOnlyDictionary<string, string> settings,
        string key)
    {
        if (settings.TryGetValue(key, out string? value))
        {
            return value;
        }

        foreach ((string candidate, string candidateValue) in settings)
        {
            if (string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase))
            {
                return candidateValue;
            }
        }

        return null;
    }
}
