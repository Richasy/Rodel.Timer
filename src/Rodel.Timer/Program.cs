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

string language = ResolveStartupLanguage(CultureInfo.CurrentUICulture);
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

static string ResolveStartupLanguage(CultureInfo culture)
{
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
