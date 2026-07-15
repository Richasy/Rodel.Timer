using System.Globalization;

namespace Rodel.Timer.Presentation;

/// <summary>
/// Stable UI Automation identifiers for the shell. Every id is prefixed <c>RodelTimer.</c> and every per-row id
/// embeds the entity's <see cref="Guid"/> (formatted <c>N</c>) so it is unique across folders and history entries.
/// These builders are pure and testable, so a test can assert the uniqueness invariant.
/// </summary>
public static class TimerAutomationIds
{
    /// <summary>The common id prefix for every automation element in the app.</summary>
    public const string Prefix = "RodelTimer";

    /// <summary>The settings button.</summary>
    public const string Settings = "RodelTimer.Main.Settings";

    /// <summary>The start/stop timer button.</summary>
    public const string StartStop = "RodelTimer.Main.StartStop";

    /// <summary>The session title text box.</summary>
    public const string Title = "RodelTimer.Main.Title";

    /// <summary>The enter/exit compact overlay button.</summary>
    public const string Compact = "RodelTimer.Main.Compact";

    /// <summary>The footer folder-pane toggle.</summary>
    public const string FoldersToggle = "RodelTimer.Main.FoldersToggle";

    /// <summary>The footer history-pane toggle.</summary>
    public const string HistoryToggle = "RodelTimer.Main.HistoryToggle";

    /// <summary>The outer (folder) split view group.</summary>
    public const string FoldersSplit = "RodelTimer.Main.FoldersSplit";

    /// <summary>The inner (history) split view group.</summary>
    public const string HistorySplit = "RodelTimer.Main.HistorySplit";

    /// <summary>The add-folder button in the folder pane.</summary>
    public const string AddFolder = "RodelTimer.Main.AddFolder";

    /// <summary>The retry button shown on a data or save error.</summary>
    public const string Retry = "RodelTimer.Main.Retry";

    /// <summary>The settings dialog surface (the <c>Pane</c> named by its live-localized title).</summary>
    public const string SettingsDialog = "RodelTimer.Settings.Dialog";

    /// <summary>The settings theme combo box.</summary>
    public const string SettingsTheme = "RodelTimer.Settings.Theme";

    /// <summary>The settings language combo box.</summary>
    public const string SettingsLanguage = "RodelTimer.Settings.Language";

    /// <summary>The settings UI-font combo box.</summary>
    public const string SettingsFont = "RodelTimer.Settings.Font";

    /// <summary>The settings daily-target number box.</summary>
    public const string SettingsDailyTarget = "RodelTimer.Settings.DailyTarget";

    /// <summary>The settings open-source link.</summary>
    public const string SettingsOpenSource = "RodelTimer.Settings.OpenSource";

    /// <summary>The move-history dialog surface (the <c>Pane</c> named by its title).</summary>
    public const string MoveHistoryDialog = "RodelTimer.MoveHistory.Dialog";

    /// <summary>The move-history target-folder combo box.</summary>
    public const string MoveHistoryFolder = "RodelTimer.MoveHistory.Folder";

    /// <summary>Builds the id for a folder row's select button.</summary>
    /// <param name="folderId">The folder's identifier.</param>
    /// <returns>The unique automation id.</returns>
    public static string FolderSelect(Guid folderId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.Folder.{folderId:N}.Select");

    /// <summary>Builds the id for a folder row's overflow ("more") button.</summary>
    /// <param name="folderId">The folder's identifier.</param>
    /// <returns>The unique automation id.</returns>
    public static string FolderMore(Guid folderId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.Folder.{folderId:N}.More");

    /// <summary>Builds the id for a history row's container.</summary>
    /// <param name="entryId">The history entry's identifier.</param>
    /// <returns>The unique automation id.</returns>
    public static string HistoryItem(Guid entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.History.{entryId:N}.Item");

    /// <summary>Builds the id for a history row's overflow ("more") button.</summary>
    /// <param name="entryId">The history entry's identifier.</param>
    /// <returns>The unique automation id.</returns>
    public static string HistoryMore(Guid entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.History.{entryId:N}.More");
}
