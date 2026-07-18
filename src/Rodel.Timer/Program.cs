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

// The default preference follows the OS preferred UI language (the "system" sentinel), mirroring the theme's System
// default. The concrete startup language is resolved in App.OnLaunched from the framework's Application.SystemLanguage
// (the OS preferred-languages list) — Program runs before the app exists and could only read CurrentUICulture (the
// Windows *display* language), the wrong signal for following the preferred language.
var defaultPreferences = new TimerPreferences(
    TimerThemeMode.System,
    TimerBackgroundMode.Mica,
    TimerPreferences.SystemLanguage,
    TimerPreferences.DefaultFontFamily,
    TimerPreferences.DefaultDailyTargetMinutes);

return SproutApp.CreateBuilder(args)
    .UseApp<App>()
    .UseD2DBackend()
    .UseSingleInstance()
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
