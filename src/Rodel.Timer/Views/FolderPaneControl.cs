using Rodel.Timer.Core.Folders;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Popups;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The visual content of a folder ListItem. The ItemsView container owns selection, focus, context gestures, and UIA;
/// this view draws only the folder glyph, name, and created date.
/// </summary>
[View]
public readonly partial record struct FolderRowView(
    string Name,
    string CreatedText,
    FluentSymbol IconSymbol,
    Brush IconBrush,
    Brush TileBrush,
    Color DateInk,
    TextFormat NameStyle,
    TextFormat DateStyle)
{
    /// <summary>The transparent row content inset from the container's selection indicator.</summary>
    public Ui Body => Border(
        Row([
            Border(
                Icon.Fluent(IconSymbol, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, IconBrush)
                    .HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center))
                .Background(TileBrush)
                .CornerRadius(TimerVisuals.Radius.App)
                .Width(TimerVisuals.IconSize.Tile)
                .Height(TimerVisuals.IconSize.Tile),
            Column([
                Text(Name, NameStyle),
                Text(CreatedText, DateStyle, DateInk)
            ]).Spacing(2f).VAlign(VerticalAlignment.Center).Weight(1f)
        ]).Spacing(10f).VAlign(VerticalAlignment.Center)).Padding(16f, 7f, 7f, 7f);
}

/// <summary>
/// A controlled single-select folder list. Stable Guid keys preserve row identity, and each container fresh-builds an
/// owned context menu for mouse, touch/pen, and keyboard context gestures.
/// </summary>
[View]
public readonly partial record struct FolderListView(
    IReadOnlyList<FolderRowData> Rows,
    Func<FolderRowData, FolderRowView> BuildRow,
    IReadOnlyList<int> SelectedIndices,
    Action<IReadOnlyList<int>> OnSelectionChanged,
    Func<FolderRowData, IFlyout> BuildContextMenu)
{
    /// <summary>The bounded, virtualized list hosted directly by the anchored flyout.</summary>
    public Ui Body => Overlay([BuildItemsView()]);

    internal ItemsView BuildItemsView() =>
        TimerRowListFactory.Folders(
            Rows,
            BuildRow,
            new StackLayout { ItemExtent = TimerVisuals.Row.Folder, Spacing = TimerVisuals.Spacing.XSmall },
            static data => new AutomationInfo
            {
                AutomationId = TimerAutomationIds.FolderSelect(data.Folder.Id),
                ItemStatus = data.IsSelected ? Resources.CurrentFolder : null,
                HelpText = Resources.FolderRowHelp,
            },
            BuildContextMenu,
            SelectedIndices,
            OnSelectionChanged);
}

/// <summary>
/// The fresh content host of the anchored Folders flyout. It owns the controlled selection callback and context-menu
/// factory for this open lifetime while the keyed ItemsView preserves row, focus, and scroll identity across rebinds.
/// </summary>
public sealed partial class FolderPaneControl : Control
{
    private readonly TimerChrome _chrome;
    private readonly Action _close;
    private readonly Action<IReadOnlyList<int>> _onSelectionChanged;
    private readonly Func<FolderRowData, IFlyout> _buildContextMenu;

    /// <summary>Creates the Folders flyout content.</summary>
    /// <param name="chrome">The live read-and-command surface.</param>
    /// <param name="close">Closes the parent flyout after a primary folder selection.</param>
    public FolderPaneControl(TimerChrome chrome, Action close)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
        _close = close ?? throw new ArgumentNullException(nameof(close));
        _onSelectionChanged = OnSelectionChanged;
        _buildContextMenu = BuildContextMenu;
    }

    /// <summary>The section header (own padding) over a divider, then the controlled folder list (own padding).</summary>
    public Ui Body => Column([
        Row([
            Column([
                Text(
                   Resources.Folders,
                   new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight)),
                Text(
                    Resources.FoldersFlyoutSubtitle,
                    new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.NormalWeight),
                    Colors.TextFillColorSecondary)
            ]).Spacing(2f).VAlign(VerticalAlignment.Center).Weight(1f),
            Button(
                Icon.Fluent(FluentSymbol.Add, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium),
                _chrome.ShowAddFolderDialog,
                TimerVisuals.Ghost(Colors))
                .Style(TimerVisuals.Buttons.Icon)
                .Width(TimerVisuals.Height.Icon)
                .Height(TimerVisuals.Height.Icon)
                .AccessKey("A")
                .ToolTip(Resources.AddFolder)
                .Automation(AddAutomation)
        ]).Spacing(TimerVisuals.Spacing.Small).Padding(16f, 13f, 14f, 11f),
        Divider.Horizontal(),
        new FolderListView(
            Rows,
            _chrome.BuildFolderRow,
            SelectedIndices,
            _onSelectionChanged,
            _buildContextMenu).Height(TimerAnchoredSurfaceLayoutResolver.ListHeight(Rows.Count))
    ]).Spacing(0f);

    private IReadOnlyList<FolderRowData> Rows => _chrome.FolderRows;

    private IReadOnlyList<int> SelectedIndices
    {
        get
        {
            IReadOnlyList<FolderRowData> rows = Rows;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].IsSelected)
                {
                    return [i];
                }
            }

            return [];
        }
    }

    internal static AutomationInfo AddAutomation => new()
    {
        Name = Resources.AddFolder,
        HelpText = Resources.AddFolderHelp,
        AutomationId = TimerAutomationIds.AddFolder,
        AccessKey = "A",
    };

    private ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }

    private void OnSelectionChanged(IReadOnlyList<int> indices)
    {
        if (indices.Count != 1)
        {
            return;
        }

        IReadOnlyList<FolderRowData> rows = Rows;
        int index = indices[0];
        if ((uint)index >= (uint)rows.Count)
        {
            return;
        }

        _chrome.SelectFolder(rows[index].Folder.Id);
        _close();
    }

    private MenuFlyout BuildContextMenu(FolderRowData data) =>
        _chrome.BuildFolderContextMenu(data.Folder.Id);
}
