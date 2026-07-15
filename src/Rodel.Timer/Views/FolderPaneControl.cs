using Rodel.Timer.Core.Folders;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// One folder row of the folder pane — a <b>multi-control composite</b>: a full-row select button (a rounded glyph tile
/// over the folder name and its created date) and a trailing 36 epx "more" button that opens the per-folder command
/// menu, both wrapped in a single rounded (radius 8) row surface, with a leading 3×24 rounded accent indicator overlaid
/// at the row's leading edge for the selected folder. Two interactive children make it project a Group three-tree, so
/// each folder is clickable, focusable and screen-reader reachable. The <b>whole row</b> (select + more) shares one
/// selected surface — a subtle <see cref="ThemeColorToken.SubtleFillColorSecondary"/> fill when selected, transparent
/// otherwise — so the highlight reads as a complete row (the more button no longer hangs outside it); the select and
/// more buttons each keep an independent ghost palette (their hover overlays the row surface) and their own hit region,
/// and the selected row carries an <see cref="AutomationInfo.ItemStatus"/>; every brush, palette and text style is
/// supplied resolved by the host, which closes over the folder id.
/// </summary>
/// <param name="Name">The folder name (the primary label).</param>
/// <param name="CreatedText">The folder's created-at secondary line.</param>
/// <param name="IconSymbol">The folder's Fluent glyph.</param>
/// <param name="IconBrush">The resolved glyph brush.</param>
/// <param name="TileBrush">The resolved glyph-tile background brush.</param>
/// <param name="IndicatorBrush">The resolved leading indicator brush (accent when selected, transparent otherwise).</param>
/// <param name="RowSurface">The resolved whole-row surface brush (subtle fill when selected, transparent otherwise).</param>
/// <param name="NameInk">The resolved name colour.</param>
/// <param name="DateInk">The resolved created-date colour.</param>
/// <param name="NameStyle">The resolved name text style.</param>
/// <param name="DateStyle">The resolved created-date text style.</param>
/// <param name="SelectPalette">The resolved select-button palette (a ghost fill; the row surface carries the selection).</param>
/// <param name="MorePalette">The resolved more-button palette.</param>
/// <param name="OnSelect">Selects this folder.</param>
/// <param name="OnMore">Opens this folder's command menu.</param>
/// <param name="SelectAutomation">The select button's accessibility info (name + id + selected status).</param>
/// <param name="MoreAutomation">The more button's accessibility info (name + id).</param>
[Composite]
public readonly partial record struct FolderRowView(
    string Name,
    string CreatedText,
    FluentSymbol IconSymbol,
    Brush IconBrush,
    Brush TileBrush,
    Brush IndicatorBrush,
    Brush RowSurface,
    Color NameInk,
    Color DateInk,
    TextFormat NameStyle,
    TextFormat DateStyle,
    ButtonPalette SelectPalette,
    ButtonPalette MorePalette,
    Action OnSelect,
    Action OnMore,
    AutomationInfo SelectAutomation,
    AutomationInfo MoreAutomation)
{
    /// <summary>A full-row select button (tile + name/date) and a trailing more button, wrapped in one rounded row surface, under a leading accent bar.</summary>
    public Ui Body => Overlay(
        Border(
            Stack(
                Button(
                    Stack(
                        Border(
                            Icon.Fluent(IconSymbol, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, IconBrush)
                                .HAlign(HorizontalAlignment.Center)
                                .VAlign(VerticalAlignment.Center))
                            .Background(TileBrush)
                            .CornerRadius(TimerVisuals.Radius.Control)
                            .Width(TimerVisuals.IconSize.Tile)
                            .Height(TimerVisuals.IconSize.Tile),
                        Stack(
                            Text(Name, NameStyle, NameInk),
                            Text(CreatedText, DateStyle, DateInk))
                            .Spacing(2f)
                            .VAlign(VerticalAlignment.Center)
                            .Weight(1f))
                        .Orientation(Orientation.Horizontal)
                        .Spacing(TimerVisuals.Spacing.Medium)
                        .VAlign(VerticalAlignment.Center),
                    OnSelect,
                    SelectPalette)
                    .CornerRadius(new CornerRadius(TimerVisuals.Radius.Control))
                    .Weight(1f)
                    .Automation(SelectAutomation),
                Button(
                    Icon.Fluent(FluentSymbol.MoreHorizontal, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, IconBrush),
                    OnMore,
                    MorePalette)
                    .Style(TimerVisuals.Buttons.Icon)
                    .Width(TimerVisuals.Height.Icon)
                    .Height(TimerVisuals.Height.Icon)
                    .VAlign(VerticalAlignment.Center)
                    .Automation(MoreAutomation))
                .Orientation(Orientation.Horizontal)
                .Spacing(TimerVisuals.Spacing.Small)
                .VAlign(VerticalAlignment.Center))
            .Background(RowSurface)
            .CornerRadius(TimerVisuals.Radius.Control),
        Rectangle(IndicatorBrush, new Size(TimerVisuals.Row.IndicatorWidth, TimerVisuals.Row.IndicatorHeight))
            .Rounded(TimerVisuals.Row.IndicatorWidth / 2f)
            .HAlign(HorizontalAlignment.Left)
            .VAlign(VerticalAlignment.Center));
}

/// <summary>
/// The virtualized, animated, Guid-keyed list of <see cref="FolderRowView"/> rows for the folder pane. It re-supplies
/// on every store or theme revision (selection / rename / add / delete / theme / language) with a fresh
/// <see cref="FolderRowData"/> list keyed by <see cref="TimerFolder.Id"/>, so survivors rebind in place — the row chrome
/// and highlight re-resolve through the same-key rebind, with <b>no</b> whole-list remount. The list is hosted directly
/// (rather than re-created behind a keyed boundary) so the nested repeater's list, scroll position, and keyboard focus
/// identity are preserved across revisions. The row template is supplied by the host (a method group on the owning
/// control) so its closures capture live state without a struct-<c>this</c> capture.
/// </summary>
/// <param name="Rows">The folder rows to render (folder + selection flag + theme revision).</param>
/// <param name="BuildRow">Builds one folder row (supplied by the host so it reads live selection/theme state).</param>
[Composite]
public readonly partial record struct FolderListView(
    IReadOnlyList<FolderRowData> Rows,
    Func<FolderRowData, FolderRowView> BuildRow)
{
    /// <summary>A scrolling, animated repeater of folder rows.</summary>
    public Ui Body => Overlay(
        ScrollView(
            ItemsRepeater(
                Rows,
                static d => d.Folder.Id,
                BuildRow,
                new StackLayout { ItemExtent = TimerVisuals.Row.Folder, Spacing = TimerVisuals.Spacing.XSmall })
            .AnimateItems())
        .Vertical(ScrollMode.Auto)
        .HorizontalScrollBar(ScrollBarVisibility.Hidden));
}

/// <summary>
/// The folder pane hosted in the outer split view's left pane — a section header (title + trailing add icon button) over
/// the Guid-keyed <see cref="FolderListView"/>.
/// </summary>
/// <remarks>
/// It is a <see cref="Control"/> (not a render-only value view) so the split view's <c>(Control, Control)</c> overload
/// wires its interaction and semantics trees — the folder rows and the add button are clickable, focusable, and
/// screen-reader reachable. It holds only the stable command-and-read <see cref="TimerChrome"/> surface and reads every
/// changing input (the folder rows, the row template, the font, and the theme) live inside <see cref="Body"/>, so its
/// own reactive effect re-renders it in place. The folder list is hosted <b>directly</b> (not re-created behind a keyed
/// boundary) so the nested repeater's list, scroll position, and keyboard focus identity are preserved across revisions;
/// re-theming and re-localization ride the same-key rebind carried by <see cref="FolderRowData.Revision"/>.
/// </remarks>
public sealed partial class FolderPaneControl : Control
{
    private readonly TimerChrome _chrome;

    /// <summary>Creates the folder pane over its command-and-read surface.</summary>
    /// <param name="chrome">The plain command-and-read surface (callbacks + live state + colours).</param>
    /// <exception cref="ArgumentNullException"><paramref name="chrome"/> is <see langword="null"/>.</exception>
    public FolderPaneControl(TimerChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
    }

    /// <summary>The section header (title + add button) and the folder list.</summary>
    public Ui Body => Stack(
        Stack(
            Text(Resources.Folders, new TextFormat(FontFamily, 18f, TextFormat.SemiBoldWeight), Colors.TextFillColorPrimary)
                .VAlign(VerticalAlignment.Center)
                .Weight(1f),
            Button(
                Icon.Fluent(FluentSymbol.Add, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, Brush.CreateSolid(Colors.TextFillColorPrimary)),
                _chrome.ShowAddFolderDialog,
                TimerVisuals.Ghost(Colors))
                .Style(TimerVisuals.Buttons.Icon)
                .Width(TimerVisuals.Height.Icon)
                .Height(TimerVisuals.Height.Icon)
                .AccessKey("A")
                .Automation(AddAutomation))
            .Orientation(Orientation.Horizontal)
            .Spacing(TimerVisuals.Spacing.Small)
            .VAlign(VerticalAlignment.Center),
        new FolderListView(_chrome.FolderRows, _chrome.BuildFolderRow)
            .Weight(1f))
        .Spacing(TimerVisuals.Spacing.Medium)
        .Padding(TimerVisuals.Spacing.Large, TimerVisuals.Spacing.Large + TimerVisuals.Spacing.XSmall, TimerVisuals.Spacing.Large, TimerVisuals.Spacing.Large);

    private string FontFamily => _chrome.FontFamily;

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
}
