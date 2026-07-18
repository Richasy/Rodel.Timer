using Sprout;

using Windows.Storage;

namespace Rodel.Timer.AppData;

/// <summary>
/// Persists the main window's placement (position, size, and maximized state) so the window reopens where the user
/// left it. Packaged builds use the app's <see cref="ApplicationData.LocalSettings"/> (a single string value); the
/// unpackaged developer build falls back to a small file under LocalAppData, so both forms remember. The stored value
/// is an opaque <c>WindowPlacement.Serialize()</c> string — the framework owns capture, DPI rescale, and multi-monitor
/// clamping on restore, so this layer never interprets it. Every read/write is fail-soft: a missing or unreadable value
/// simply means "no saved placement" and the window opens at its default centered size.
/// </summary>
internal static class WindowPlacementStore
{
    private const string SettingKey = "WindowPlacement";
    private const string FileName = "window-placement.txt";

    /// <summary>Loads the saved placement string, or <see langword="null"/> if none is stored or it cannot be read.</summary>
    public static string? Load()
    {
        try
        {
            if (PackageIdentity.IsPackaged)
            {
                return ApplicationData.Current.LocalSettings.Values.TryGetValue(SettingKey, out object? value)
                    ? value as string
                    : null;
            }

            string path = UnpackagedPath();
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Saves the placement string. Fail-soft: an I/O or access error is swallowed (placement is best-effort).</summary>
    public static void Save(string placement)
    {
        if (string.IsNullOrEmpty(placement))
        {
            return;
        }

        try
        {
            if (PackageIdentity.IsPackaged)
            {
                ApplicationData.Current.LocalSettings.Values[SettingKey] = placement;
                return;
            }

            string path = UnpackagedPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, placement);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string UnpackagedPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rodel.Timer",
        FileName);
}
