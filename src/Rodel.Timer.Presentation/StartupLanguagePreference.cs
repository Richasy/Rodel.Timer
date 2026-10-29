using Rodel.Timer.Core.Persistence;

namespace Rodel.Timer.Presentation;

/// <summary>Reads the optional startup language without preventing the main data-load error surface from opening.</summary>
public static class StartupLanguagePreference
{
    /// <summary>Peeks a saved workspace's language; missing or unreadable data falls back to the system language.</summary>
    /// <param name="filePath">The persisted workspace file.</param>
    /// <returns>The saved language, or <see langword="null"/> when the normal bootstrap must handle the file.</returns>
    public static string? Read(string filePath)
    {
        try
        {
            using var repository = new TimerDataRepository(filePath);
            return repository.LoadAsync().GetAwaiter().GetResult()?.Preferences.Language;
        }
        catch (TimerDataException)
        {
            return null;
        }
    }
}
