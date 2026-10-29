using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;
using Rodel.Timer.Tests.TestSupport;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class StartupLanguagePreferenceTests
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"timer-startup-language-{Guid.NewGuid():N}");

    [TestInitialize]
    public void Initialize() => Directory.CreateDirectory(_directory);

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_directory, recursive: true);

    [TestMethod]
    public void MissingFile_FollowsSystemWithoutCreatingData()
    {
        string path = Path.Combine(_directory, "workspace.json");

        Assert.IsNull(StartupLanguagePreference.Read(path));
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public void CorruptFile_FollowsSystemAndPreservesTheOriginalForBootstrap()
    {
        string path = Path.Combine(_directory, "workspace.json");
        const string corruptData = "{broken workspace";
        File.WriteAllText(path, corruptData);

        Assert.IsNull(StartupLanguagePreference.Read(path));
        Assert.AreEqual(corruptData, File.ReadAllText(path));
        using var repository = new TimerDataRepository(path);
        Assert.Throws<TimerDataCorruptedException>(() => repository.LoadAsync().GetAwaiter().GetResult());
    }

    [TestMethod]
    public void LockedFile_FollowsSystemAndPreservesTheOriginal()
    {
        string path = Path.Combine(_directory, "workspace.json");
        File.WriteAllText(path, "existing data");
        using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.IsNull(StartupLanguagePreference.Read(path));
        }

        Assert.AreEqual("existing data", File.ReadAllText(path));
    }

    [TestMethod]
    public async Task ValidFile_UsesThePersistedLanguage()
    {
        string path = Path.Combine(_directory, "workspace.json");
        using var harness = new TimerStoreHarness();
        harness.LoadToReady();
        harness.Store.UpdateLanguage(TimerPreferences.TraditionalChineseLanguage);
        using var repository = new TimerDataRepository(path);
        await repository.SaveAsync(harness.Store.Bootstrap.Value!.Workspace);

        Assert.AreEqual(TimerPreferences.TraditionalChineseLanguage, StartupLanguagePreference.Read(path));
    }
}
