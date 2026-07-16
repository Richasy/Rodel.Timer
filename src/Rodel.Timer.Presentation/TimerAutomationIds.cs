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

    /// <summary>The title-bar Folders drop-down button.</summary>
    public const string FoldersToggle = "RodelTimer.Main.FoldersToggle";

    /// <summary>The title-bar History drop-down button.</summary>
    public const string HistoryToggle = "RodelTimer.Main.HistoryToggle";

    /// <summary>The Folders flyout Pane.</summary>
    public const string FoldersFlyout = "RodelTimer.Folders.Flyout";

    /// <summary>The Folders flyout content Group.</summary>
    public const string FoldersFlyoutContent = "RodelTimer.Folders.Content";

    /// <summary>The History flyout Pane.</summary>
    public const string HistoryFlyout = "RodelTimer.History.Flyout";

    /// <summary>The History flyout content Group.</summary>
    public const string HistoryFlyoutContent = "RodelTimer.History.Content";

    /// <summary>The add-folder button in the folder pane.</summary>
    public const string AddFolder = "RodelTimer.Main.AddFolder";

    /// <summary>The retry button shown on a data or save error.</summary>
    public const string Retry = "RodelTimer.Main.Retry";

    /// <summary>The settings dialog surface (the <c>Pane</c> named by its live-localized title).</summary>
    public const string SettingsDialog = "RodelTimer.Settings.Dialog";

    /// <summary>The settings theme combo box.</summary>
    public const string SettingsTheme = "RodelTimer.Settings.Theme";

    /// <summary>The settings window-background combo box.</summary>
    public const string SettingsBackground = "RodelTimer.Settings.Background";

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

    /// <summary>Builds the id for a folder row's context menu.</summary>
    /// <param name="folderId">The folder's identifier.</param>
    /// <returns>The unique automation id.</returns>
    public static string FolderMenu(Guid folderId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.Folder.{folderId:N}.Menu");

    /// <summary>Builds the id for a folder context menu's Edit command.</summary>
    public static string FolderEdit(Guid folderId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.Folder.{folderId:N}.Edit");

    /// <summary>Builds the id for a folder context menu's Delete command.</summary>
    public static string FolderDelete(Guid folderId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.Folder.{folderId:N}.Delete");

    /// <summary>Builds the id for a history row's container.</summary>
    /// <param name="entryId">The history entry's identifier.</param>
    /// <returns>The unique automation id.</returns>
    public static string HistoryItem(Guid entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.History.{entryId:N}.Item");

    /// <summary>Builds the id for a history row's context menu.</summary>
    /// <param name="entryId">The history entry's identifier.</param>
    /// <returns>The unique automation id.</returns>
    public static string HistoryMenu(Guid entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.History.{entryId:N}.Menu");

    /// <summary>Builds the id for a history context menu's Move command.</summary>
    public static string HistoryMove(Guid entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.History.{entryId:N}.Move");

    /// <summary>Builds the id for a history context menu's Delete command.</summary>
    public static string HistoryDelete(Guid entryId) =>
        string.Create(CultureInfo.InvariantCulture, $"RodelTimer.History.{entryId:N}.Delete");
}
