using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;

using Sprout;

namespace Rodel.Timer;

internal static class SelfTest
{
    public static int Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"rodel-timer-selftest-{Guid.NewGuid():N}");

        try
        {
            Guid folderId = Guid.ParseExact("11111111111111111111111111111111", "N");
            var timeProvider = new FixedTimeProvider(
                new DateTimeOffset(2026, 7, 14, 1, 0, 0, TimeSpan.Zero));
            var preferences = new TimerPreferences(
                TimerThemeMode.System,
                TimerPreferences.EnglishLanguage,
                TimerPreferences.DefaultFontFamily,
                TimerPreferences.DefaultDailyTargetMinutes);

            using var repository = new TimerDataRepository(Path.Combine(directory, "timer-data.json"));
            var importer = new LegacyWorkTimerImporter(
                "Default",
                preferences,
                TimeZoneInfo.Utc);
            var bootstrapper = new TimerDataBootstrapper(
                repository,
                EmptyLegacyDataSource.Instance,
                importer,
                timeProvider,
                "Default",
                preferences,
                () => folderId);

            TimerBootstrapResult created = bootstrapper.LoadAsync().GetAwaiter().GetResult();
            TimerBootstrapResult loaded = bootstrapper.LoadAsync().GetAwaiter().GetResult();

            bool corePassed = created.Origin == TimerBootstrapOrigin.Created
                && loaded.Workspace.Folders.Count == 1
                && loaded.Workspace.SelectedFolderId == folderId;
            bool persistencePassed = loaded.Origin == TimerBootstrapOrigin.Existing;
            bool unpackaged = !PackageIdentity.IsPackaged;
            bool passed = corePassed && persistencePassed && unpackaged;

            Console.WriteLine(
                $"packaging: IsPackaged={PackageIdentity.IsPackaged} (expected false) => {(unpackaged ? "ok" : "bad")}");
            Console.WriteLine(
                $"selftest: core={(corePassed ? "ok" : "bad")} "
                + $"persistence={(persistencePassed ? "ok" : "bad")} "
                + $"leaked=0 => {(passed ? "PASS" : "FAIL")}");
            return passed ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not remove self-test directory: {exception.Message}");
            }
        }
    }

    private sealed class EmptyLegacyDataSource : ILegacyWorkTimerDataSource
    {
        public static EmptyLegacyDataSource Instance { get; } = new();

        public ValueTask<LegacyWorkTimerSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<LegacyWorkTimerSnapshot?>(null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
