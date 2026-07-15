using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Coverage of <see cref="TimerAutomationIds"/> — every id is prefixed <c>RodelTimer.</c> and every per-row id embeds
/// the entity <see cref="Guid"/> so it is unique across folders and history entries (the invariant a screen reader and
/// UI-test driver rely on to address one row unambiguously).
/// </summary>
[TestClass]
public sealed class TimerAutomationIdsTests
{
    [TestMethod]
    public void EveryStaticIdCarriesThePrefix()
    {
        foreach (string id in StaticIds)
        {
            Assert.StartsWith($"{TimerAutomationIds.Prefix}.", id, $"'{id}' must carry the app prefix.");
        }
    }

    [TestMethod]
    public void StaticIdsAreAllDistinct() =>
        Assert.AreEqual(StaticIds.Length, StaticIds.Distinct(StringComparer.Ordinal).Count(), "no two shell ids may collide.");

    private static readonly string[] StaticIds =
    [
        TimerAutomationIds.Settings,
        TimerAutomationIds.StartStop,
        TimerAutomationIds.Title,
        TimerAutomationIds.Compact,
        TimerAutomationIds.FoldersToggle,
        TimerAutomationIds.HistoryToggle,
        TimerAutomationIds.FoldersSplit,
        TimerAutomationIds.HistorySplit,
        TimerAutomationIds.AddFolder,
        TimerAutomationIds.Retry,
        TimerAutomationIds.SettingsDialog,
        TimerAutomationIds.SettingsTheme,
        TimerAutomationIds.SettingsLanguage,
        TimerAutomationIds.SettingsFont,
        TimerAutomationIds.SettingsDailyTarget,
        TimerAutomationIds.SettingsOpenSource,
        TimerAutomationIds.MoveHistoryDialog,
        TimerAutomationIds.MoveHistoryFolder,
    ];

    [TestMethod]
    public void PerRowIdsAreUniqueAcrossManyDistinctEntities()
    {
        List<string> ids = [];
        for (int i = 0; i < 200; i++)
        {
            Guid folderId = Guid.NewGuid();
            Guid entryId = Guid.NewGuid();
            ids.Add(TimerAutomationIds.FolderSelect(folderId));
            ids.Add(TimerAutomationIds.FolderMore(folderId));
            ids.Add(TimerAutomationIds.HistoryItem(entryId));
            ids.Add(TimerAutomationIds.HistoryMore(entryId));
        }

        Assert.AreEqual(ids.Count, ids.Distinct(StringComparer.Ordinal).Count(), "every per-row id must be unique.");
    }

    [TestMethod]
    public void SameEntityProducesDistinctSelectAndMoreIds()
    {
        Guid folderId = Guid.NewGuid();

        Assert.AreNotEqual(TimerAutomationIds.FolderSelect(folderId), TimerAutomationIds.FolderMore(folderId));
    }

    [TestMethod]
    public void SameEntityProducesStableIds()
    {
        Guid folderId = Guid.NewGuid();

        Assert.AreEqual(TimerAutomationIds.FolderSelect(folderId), TimerAutomationIds.FolderSelect(folderId));
    }

    [TestMethod]
    public void PerRowIdsEmbedTheGuidAndCarryThePrefix()
    {
        Guid folderId = Guid.NewGuid();
        string id = TimerAutomationIds.FolderSelect(folderId);

        Assert.StartsWith($"{TimerAutomationIds.Prefix}.", id);
        Assert.Contains(folderId.ToString("N"), id, StringComparison.Ordinal);
    }
}
