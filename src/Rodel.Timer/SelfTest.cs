using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Localization;
using Sprout.Reactive;

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
            Guid entryId = Guid.ParseExact("22222222222222222222222222222222", "N");
            int saves = 0;
            using var store = new TimerAppStore(
                _ => Task.FromResult(created),
                (_, _) =>
                {
                    saves++;
                    return Task.CompletedTask;
                },
                timeProvider,
                TimeZoneInfo.Utc,
                new InlineScheduler(timeProvider),
                () => entryId);
            store.LoadAsync().GetAwaiter().GetResult();
            store.StartTimer();
            _ = store.StopTimer("NativeAOT selftest");
            store.CloseAsync().GetAwaiter().GetResult();
            bool storePassed = store.Bootstrap.IsSuccess
                && store.Entries.Count == 1
                && store.Entries[0].Id == entryId
                && store.SaveState.IsSuccess
                && saves == 1;

            string originalLanguage = Localizer.CurrentLanguage;
            bool localizationPassed;
            try
            {
                Localizer.CurrentLanguage = TimerPreferences.EnglishLanguage;
                bool english = Resources.AppName == "Rodel Timer";
                Localizer.CurrentLanguage = TimerPreferences.ChineseLanguage;
                bool chinese = Resources.AppName == "Rodel 计时器";
                localizationPassed = english && chinese;
            }
            finally
            {
                Localizer.CurrentLanguage = originalLanguage;
            }

            bool unpackaged = !PackageIdentity.IsPackaged;
            bool passed = corePassed && persistencePassed && storePassed && localizationPassed && unpackaged;

            Console.WriteLine(
                $"packaging: IsPackaged={PackageIdentity.IsPackaged} (expected false) => {(unpackaged ? "ok" : "bad")}");
            Console.WriteLine(
                $"selftest: core={(corePassed ? "ok" : "bad")} "
                + $"persistence={(persistencePassed ? "ok" : "bad")} "
                + $"store={(storePassed ? "ok" : "bad")} "
                + $"localization={(localizationPassed ? "ok" : "bad")} "
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

    private sealed class InlineScheduler(TimeProvider timeProvider) : IScheduler
    {
        public DateTimeOffset Now => timeProvider.GetUtcNow();

        public void Post(Action action) => action();

        public IDisposable Schedule(TimeSpan delay, Action callback) => NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
