using System.Runtime.InteropServices;

using Rodel.Timer.Core.Migration;

using Sprout;

using Windows.Storage;

namespace Rodel.Timer.AppData;

internal sealed class PackagedLegacyWorkTimerDataSource : ILegacyWorkTimerDataSource
{
    private const string FolderListFileName = "FolderList.json";
    private const string HistoryListFileName = "HistoryList.json";
    private const string SettingsContainerName = "WorkTimer";

    private static readonly string[] SettingKeys =
    [
        "Theme",
        "AppVersion",
        "Language",
        "FontFamily",
        "LastSelectFolderId",
        "PresetDuration",
    ];

    public async ValueTask<LegacyWorkTimerSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!PackageIdentity.IsPackaged)
        {
            return null;
        }

        try
        {
            ApplicationData appData = ApplicationData.Current;
            string localFolder = appData.LocalFolder.Path;

            string? folderListJson = await ReadIfPresentAsync(
                    Path.Combine(localFolder, FolderListFileName),
                    cancellationToken)
                .ConfigureAwait(false);
            string? historyListJson = await ReadIfPresentAsync(
                    Path.Combine(localFolder, HistoryListFileName),
                    cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyDictionary<string, string> settings = ReadSettings(appData.LocalSettings);

            LegacyWorkTimerSnapshot snapshot = new(folderListJson, historyListJson, settings);
            return snapshot.HasData ? snapshot : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or COMException)
        {
            throw new LegacyDataAccessException(
                "The archived Work-Timer data could not be read from packaged application storage.",
                exception);
        }
    }

    private static async Task<string?> ReadIfPresentAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string> ReadSettings(
        ApplicationDataContainer localSettings)
    {
        Dictionary<string, string> settings = new(StringComparer.Ordinal);
        if (!localSettings.Containers.TryGetValue(
                SettingsContainerName,
                out ApplicationDataContainer? container))
        {
            return settings;
        }

        foreach (string key in SettingKeys)
        {
            if (container.Values.TryGetValue(key, out object? value)
                && value?.ToString() is { Length: > 0 } text)
            {
                settings[key] = text;
            }
        }

        return settings;
    }
}
