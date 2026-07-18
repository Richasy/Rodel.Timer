using System.Globalization;

using Rodel.Timer;
using Rodel.Timer.AppData;
using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Backend.D2D;

if (args.Contains("--selftest", StringComparer.OrdinalIgnoreCase))
{
    return SelfTest.Run();
}

string language = ResolveStartupLanguage(TryReadPersistedLanguage(), CultureInfo.CurrentUICulture);
var defaultPreferences = new TimerPreferences(
    TimerThemeMode.System,
    TimerBackgroundMode.Mica,
    language,
    TimerPreferences.DefaultFontFamily,
    TimerPreferences.DefaultDailyTargetMinutes);

return SproutApp.CreateBuilder(args)
    .UseApp<App>()
    .UseD2DBackend()
    .UseSingleInstance()
    .UseLanguage(language)
    .ConfigureServices(services =>
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<ILegacyWorkTimerDataSource>(new PackagedLegacyWorkTimerDataSource());
        services.AddSingleton(_ => new TimerDataRepository(AppDataPaths.TimerDataFile()));
        services.AddSingleton(_ => new LegacyWorkTimerImporter(
            Resources.DefaultFolder,
            defaultPreferences,
            TimeZoneInfo.Local));
        services.AddSingleton(serviceProvider => new TimerDataBootstrapper(
            serviceProvider.GetRequiredService<TimerDataRepository>(),
            serviceProvider.GetRequiredService<ILegacyWorkTimerDataSource>(),
            serviceProvider.GetRequiredService<LegacyWorkTimerImporter>(),
            serviceProvider.GetRequiredService<TimeProvider>(),
            Resources.DefaultFolder,
            defaultPreferences));
    })
    .Build()
    .Run();

static string ResolveStartupLanguage(string? persisted, CultureInfo culture)
{
    // A returning user's saved CONCRETE display language wins, so the UI mounts in it from the first frame. A
    // follow-system preference (the sentinel) or a first run (no saved language) resolves the OS preferred language
    // instead — and is then re-resolved live if the OS language changes (see App's SystemLanguageChanged handler).
    if (!string.IsNullOrWhiteSpace(persisted) && !LanguageResolver.IsFollowSystem(persisted))
    {
        return persisted;
    }

    return LanguageResolver.ResolveSupported(culture);
}

// Peek the persisted display language synchronously before the app mounts, so the UI mounts in the saved language.
// A missing workspace (first run) or an unreadable/corrupt file yields null — the caller then falls back to the OS
// culture, and bootstrap surfaces any real read failure later.
static string? TryReadPersistedLanguage()
{
    try
    {
        using var repository = new TimerDataRepository(AppDataPaths.TimerDataFile());
        var workspace = repository.LoadAsync().GetAwaiter().GetResult();
        return workspace?.Preferences.Language;
    }
    catch (TimerDataAccessException)
    {
        return null;
    }
}
