using System.Globalization;

using Rodel.Timer;
using Rodel.Timer.AppData;
using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;

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
    // A returning user's SAVED display language wins, so the UI mounts in it from the first frame. Otherwise the app
    // mounts in the OS-culture language and only re-applies the saved language after the workspace loads, which leaves
    // any hoisted (mount-time-constant) localized string — e.g. the stage caption / primary-action label read directly
    // in a nested composite — stuck in the OS-culture language. First run (no saved workspace) falls through to the
    // OS culture below.
    if (!string.IsNullOrWhiteSpace(persisted))
    {
        return persisted;
    }

    for (CultureInfo current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
    {
        if (current.Name.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase))
        {
            return TimerPreferences.TraditionalChineseLanguage;
        }

        if (current.Name.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase))
        {
            return TimerPreferences.ChineseLanguage;
        }
    }

    return culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
        ? TimerPreferences.ChineseLanguage
        : TimerPreferences.EnglishLanguage;
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
