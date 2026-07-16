using System.Globalization;

using Rodel.Timer;
using Rodel.Timer.AppData;
using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;

using Sprout;
using Sprout.Backend.D2D;
using Sprout.LiquidGlass;

if (args.Contains("--selftest", StringComparer.OrdinalIgnoreCase))
{
    return SelfTest.Run();
}

string language = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
    ? TimerPreferences.ChineseLanguage
    : TimerPreferences.EnglishLanguage;
string defaultFolderName = language == TimerPreferences.ChineseLanguage ? "默认" : "Default";
var defaultPreferences = new TimerPreferences(
    TimerThemeMode.System,
    TimerBackgroundMode.SystemMaterial,
    language,
    TimerPreferences.DefaultFontFamily,
    TimerPreferences.DefaultDailyTargetMinutes);

return SproutApp.CreateBuilder(args)
    .UseApp<App>()
    .UseD2DBackend()
    .UseLiquidGlass()
    .UseSingleInstance()
    .UseLanguage(language)
    .ConfigureServices(services =>
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<ILegacyWorkTimerDataSource>(new PackagedLegacyWorkTimerDataSource());
        services.AddSingleton(_ => new TimerDataRepository(AppDataPaths.TimerDataFile()));
        services.AddSingleton(_ => new LegacyWorkTimerImporter(
            defaultFolderName,
            defaultPreferences,
            TimeZoneInfo.Local));
        services.AddSingleton(serviceProvider => new TimerDataBootstrapper(
            serviceProvider.GetRequiredService<TimerDataRepository>(),
            serviceProvider.GetRequiredService<ILegacyWorkTimerDataSource>(),
            serviceProvider.GetRequiredService<LegacyWorkTimerImporter>(),
            serviceProvider.GetRequiredService<TimeProvider>(),
            defaultFolderName,
            defaultPreferences));
    })
    .Build()
    .Run();
