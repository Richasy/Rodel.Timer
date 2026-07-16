using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Controls;
using Sprout.Theming;

namespace Rodel.Timer;

/// <summary>
/// A plain (non-view) read-and-command surface the <see cref="MainView"/> hands to its slot controls. A slot control
/// cannot receive the view-typed <see cref="MainView"/> directly (the reconciler forbids a view-typed constructor
/// argument on a child control), so this holder exposes exactly the live reactive state and callbacks the slot controls
/// need as plain members. It is created once and kept as a stable reference; every read here delegates to the reactive
/// <see cref="MainView"/> (or its store), so a slot control that reads one of these members inside its body still
/// auto-subscribes and re-renders when the underlying reactive state changes — the plain pass-through getter does not
/// break reactive dependency tracking.
/// </summary>
public sealed class TimerChrome
{
    private readonly Window _window;
    private readonly MainView _main;

    /// <summary>Creates the chrome over the hosting window and the root view's read-and-command surface.</summary>
    /// <param name="window">The hosting window (the source of the live theme colours).</param>
    /// <param name="main">The state-owning root view whose live state and command methods are exposed.</param>
    internal TimerChrome(Window window, MainView main)
    {
        _window = window;
        _main = main;
        ShowSettings = main.ShowSettings;
        StartOrStop = main.StartOrStop;
        ToggleCompact = main.ToggleCompact;
        ShowAddFolderDialog = main.ShowAddFolderDialog;
        Retry = main.Retry;
        SetSessionTitle = main.SetSessionTitle;
        SelectFolder = main.SelectFolder;
        BuildFolderContextMenu = main.BuildFolderContextMenu;
        BuildHistoryContextMenu = main.BuildHistoryContextMenu;
    }

    /// <summary>The live theme colours resolved from the hosting window's current theme.</summary>
    public ThemeColors Colors => _window.Theme.Colors;

    /// <summary>A monotonic stamp bumped on every theme or language change so slot controls re-resolve (reactive).</summary>
    public int ThemeStamp => _main.ThemeStamp;

    /// <summary>Whether the window is in the compact overlay presenter (reactive).</summary>
    public bool IsCompact => _main.IsCompactPresenter;

    /// <summary>The current client width in effective pixels (reactive).</summary>
    public double ViewportWidth => _main.ViewportWidth;

    /// <summary>The current client height in effective pixels (reactive).</summary>
    public double ViewportHeight => _main.ViewportHeight;

    /// <summary>The in-progress session title being edited (reactive, UI-only until a session stops).</summary>
    public string SessionTitle => _main.SessionTitle;

    /// <summary>The running session's elapsed time, or zero when idle (reactive).</summary>
    public TimeSpan Elapsed => _main.Elapsed;

    /// <summary>Whether a timing session is currently running (reactive).</summary>
    public bool IsRunning => _main.IsRunning;

    /// <summary>The selected folder's name shown above the clock (reactive).</summary>
    public string SelectedFolderName => _main.SelectedFolderName;

    /// <summary>The footer "today's total / target" summary line (reactive, culture-formatted).</summary>
    public string SummaryText => _main.SummaryText;

    /// <summary>The footer's completed/target focus value (reactive, culture-formatted).</summary>
    public string FocusSummaryText => _main.FocusSummaryText;

    /// <summary>The requested window background and its active/fallback state (reactive).</summary>
    public string BackgroundStatusText => _main.BackgroundStatusText;

    /// <summary>The current folders (reactive; a fresh reference on every store revision).</summary>
    public IReadOnlyList<TimerFolder> Folders => _main.Folders;

    /// <summary>The selected folder's history entries (reactive; a fresh reference on every store revision).</summary>
    public IReadOnlyList<TimerEntry> SelectedEntries => _main.SelectedEntries;

    /// <summary>
    /// The folder rows projected for the list repeater — one <see cref="FolderRowData"/> per folder, carrying the
    /// selection flag and the current <see cref="ThemeStamp"/> so a selection move or a theme/language switch rebinds the
    /// affected rows in place (the same-key rebind), with no whole-list keyed remount. Reactive: reading it subscribes to
    /// the folders, the selected folder, and the theme stamp.
    /// </summary>
    public IReadOnlyList<FolderRowData> FolderRows =>
        TimerRowProjector.FolderRows(_main.Folders, _main.SelectedFolderId, _main.ThemeStamp);

    /// <summary>
    /// The history rows projected for the list repeater — one <see cref="HistoryRowData"/> per entry, carrying the
    /// current <see cref="ThemeStamp"/> so a theme/language switch rebinds the rows in place. Reactive: reading it
    /// subscribes to the selected folder's entries and the theme stamp.
    /// </summary>
    public IReadOnlyList<HistoryRowData> HistoryRows =>
        TimerRowProjector.HistoryRows(_main.SelectedEntries, _main.ThemeStamp);

    /// <summary>Builds one folder row from projected row data + live theme state (the folder list's row template).</summary>
    /// <param name="data">The folder row data (folder + selection flag + theme revision).</param>
    /// <returns>The resolved <see cref="FolderRowView"/> for the folder.</returns>
    public FolderRowView BuildFolderRow(FolderRowData data) => _main.BuildFolderRow(data);

    /// <summary>Builds one history card from projected row data + live theme state (the history list's row template).</summary>
    /// <param name="data">The history row data (entry + theme revision).</param>
    /// <returns>The resolved <see cref="HistoryRowView"/> for the entry.</returns>
    public HistoryRowView BuildHistoryRow(HistoryRowData data) => _main.BuildHistoryRow(data);

    /// <summary>Opens the settings dialog.</summary>
    public Action ShowSettings { get; }

    /// <summary>Starts or stops the timer.</summary>
    public Action StartOrStop { get; }

    /// <summary>Toggles the compact overlay presenter.</summary>
    public Action ToggleCompact { get; }

    /// <summary>Opens the add-folder dialog.</summary>
    public Action ShowAddFolderDialog { get; }

    /// <summary>Retries the failed load or save operation.</summary>
    public Action Retry { get; }

    /// <summary>Sets the in-progress session title.</summary>
    public Action<string> SetSessionTitle { get; }

    /// <summary>Selects the folder with the given id.</summary>
    public Action<Guid> SelectFolder { get; }

    /// <summary>Builds a fresh context menu for a folder row.</summary>
    public Func<Guid, MenuFlyout> BuildFolderContextMenu { get; }

    /// <summary>Builds a fresh context menu for a history row.</summary>
    public Func<Guid, MenuFlyout> BuildHistoryContextMenu { get; }
}
