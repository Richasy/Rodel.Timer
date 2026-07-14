using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Tests.TestSupport;

namespace Rodel.Timer.Core.Tests.Bootstrap;

[TestClass]
public sealed class TimerDataBootstrapperTests
{
    [TestMethod]
    public async Task LoadAsync_MigratesLegacyDataOnlyOnce()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new TimerDataRepository(directory.File("timer-data.json"));
        Guid legacyFolderId = Guid.ParseExact(
            "11111111111111111111111111111111",
            "N");
        var source = new StubLegacyDataSource(new LegacyWorkTimerSnapshot(
            $$"""
            [
              {
                "Name": "Legacy",
                "CreateTime": "2022-01-02T03:04:05Z",
                "Id": "{{legacyFolderId:N}}",
                "Icon": 59648
              }
            ]
            """,
            "[]",
            new Dictionary<string, string>()));
        TimerDataBootstrapper bootstrapper = CreateBootstrapper(
            repository,
            source,
            Guid.NewGuid());

        TimerBootstrapResult first = await bootstrapper.LoadAsync();
        TimerBootstrapResult second = await bootstrapper.LoadAsync();

        Assert.AreEqual(TimerBootstrapOrigin.Migrated, first.Origin);
        Assert.AreEqual(TimerBootstrapOrigin.Existing, second.Origin);
        Assert.AreEqual(1, source.ReadCount);
        Assert.AreEqual(legacyFolderId, second.Workspace.SelectedFolderId);
    }

    [TestMethod]
    public async Task LoadAsync_CreatesAndPersistsDefaultsWhenNoLegacyDataExists()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new TimerDataRepository(directory.File("timer-data.json"));
        Guid folderId = Guid.ParseExact("11111111111111111111111111111111", "N");
        var source = new StubLegacyDataSource(null);
        TimerDataBootstrapper bootstrapper = CreateBootstrapper(
            repository,
            source,
            folderId);

        TimerBootstrapResult result = await bootstrapper.LoadAsync();
        TimerBootstrapResult loaded = await bootstrapper.LoadAsync();

        Assert.AreEqual(TimerBootstrapOrigin.Created, result.Origin);
        Assert.AreEqual(folderId, result.Workspace.SelectedFolderId);
        Assert.AreEqual("Default", result.Workspace.Folders[0].Name);
        Assert.AreEqual(TimerBootstrapOrigin.Existing, loaded.Origin);
        Assert.AreEqual(1, source.ReadCount);
    }

    [TestMethod]
    public async Task LoadAsync_DoesNotCreateNewDataWhenMigrationFails()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("timer-data.json");
        using var repository = new TimerDataRepository(path);
        var source = new StubLegacyDataSource(new LegacyWorkTimerSnapshot(
            "{ invalid",
            "[]",
            new Dictionary<string, string>()));
        TimerDataBootstrapper bootstrapper = CreateBootstrapper(
            repository,
            source,
            Guid.NewGuid());

        await Assert.ThrowsAsync<LegacyDataMigrationException>(
            () => bootstrapper.LoadAsync());

        Assert.IsFalse(File.Exists(path));
    }

    private static TimerDataBootstrapper CreateBootstrapper(
        TimerDataRepository repository,
        ILegacyWorkTimerDataSource source,
        Guid generatedId)
    {
        var timeProvider = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 14, 1, 0, 0, TimeSpan.Zero));
        var importer = new LegacyWorkTimerImporter(
            "Default",
            TimerPreferences.Default,
            TimeZoneInfo.Utc);

        return new TimerDataBootstrapper(
            repository,
            source,
            importer,
            timeProvider,
            "Default",
            TimerPreferences.Default,
            () => generatedId);
    }
}
