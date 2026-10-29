using Rodel.Timer.Presentation;

using Sprout;

using Windows.Storage;

namespace Rodel.Timer.AppData;

/// <summary>
/// Persists the standard and compact-overlay placements independently. Packaged builds use
/// <see cref="ApplicationData.LocalSettings"/>; unpackaged developer builds use separate files under LocalAppData. Each
/// stored value remains an opaque <c>WindowPlacement.Serialize()</c> string — the framework owns capture, DPI rescale,
/// and multi-monitor clamping on restore. Every read/write is fail-soft: a missing or unreadable value means that mode
/// uses its default placement.
/// </summary>
internal static class WindowPlacementStore
{
    private const string LegacySettingKey = "WindowPlacement";
    private const string LegacyFileName = "window-placement.txt";
    private const string OverlappedSettingKey = "WindowPlacement.Overlapped";
    private const string OverlappedFileName = "window-placement-overlapped.txt";
    private const string CompactOverlaySettingKey = "WindowPlacement.CompactOverlay";
    private const string CompactOverlayFileName = "window-placement-compact-overlay.txt";

    /// <summary>Loads a mode-specific placement string, or <see langword="null"/> when none can be read.</summary>
    public static string? Load(TimerWindowPlacementMode mode) =>
        LoadCore(SettingKey(mode), FileName(mode));

    /// <summary>Loads the pre-split shared placement for one-time migration.</summary>
    public static string? LoadLegacy() => LoadCore(LegacySettingKey, LegacyFileName);

    /// <summary>Saves a mode-specific placement string. Placement persistence remains best-effort.</summary>
    /// <returns><see langword="true"/> when the value was stored; otherwise <see langword="false"/>.</returns>
    public static bool Save(TimerWindowPlacementMode mode, string placement)
    {
        if (string.IsNullOrEmpty(placement))
        {
            return false;
        }

        try
        {
            if (PackageIdentity.IsPackaged)
            {
                ApplicationData.Current.LocalSettings.Values[SettingKey(mode)] = placement;
                return true;
            }

            string path = UnpackagedPath(FileName(mode));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, placement);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Removes the pre-split shared value after it has been migrated successfully.</summary>
    public static void RemoveLegacy()
    {
        try
        {
            if (PackageIdentity.IsPackaged)
            {
                _ = ApplicationData.Current.LocalSettings.Values.Remove(LegacySettingKey);
                return;
            }

            File.Delete(UnpackagedPath(LegacyFileName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string? LoadCore(string settingKey, string fileName)
    {
        try
        {
            if (PackageIdentity.IsPackaged)
            {
                return ApplicationData.Current.LocalSettings.Values.TryGetValue(settingKey, out object? value)
                    ? value as string
                    : null;
            }

            string path = UnpackagedPath(fileName);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string SettingKey(TimerWindowPlacementMode mode) => mode switch
    {
        TimerWindowPlacementMode.Overlapped => OverlappedSettingKey,
        TimerWindowPlacementMode.CompactOverlay => CompactOverlaySettingKey,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static string FileName(TimerWindowPlacementMode mode) => mode switch
    {
        TimerWindowPlacementMode.Overlapped => OverlappedFileName,
        TimerWindowPlacementMode.CompactOverlay => CompactOverlayFileName,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static string UnpackagedPath(string fileName) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rodel.Timer",
        fileName);
}
