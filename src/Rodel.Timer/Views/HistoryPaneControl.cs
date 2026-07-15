using Rodel.Timer.Core.History;
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
/// One history row of the history pane — a bordered card with a leading clock-glyph tile, the session title over a
/// duration + created-at secondary line, and a trailing 36 epx "more" button that opens the per-session command menu
/// (move / delete). The single interactive child (the more button) makes it clickable + accessible; the card background
/// and stroke re-theme through deferred theme-token brushes, while the tile brush and the more-button palette are
/// supplied resolved by the host.
/// </summary>
/// <param name="Title">The session title (the primary label).</param>
/// <param name="Secondary">The duration + created-at secondary line.</param>
/// <param name="TitleInk">The resolved title colour.</param>
/// <param name="SecondaryInk">The resolved secondary colour.</param>
/// <param name="TitleStyle">The resolved title text style.</param>
/// <param name="SecondaryStyle">The resolved secondary text style.</param>
/// <param name="IconBrush">The resolved clock-glyph brush.</param>
/// <param name="TileBrush">The resolved glyph-tile background brush.</param>
/// <param name="MorePalette">The resolved more-button palette.</param>
/// <param name="OnMore">Opens this session's command menu.</param>
/// <param name="MoreAutomation">The more button's accessibility info (name + id + full description).</param>
[Composite]
public readonly partial record struct HistoryRowView(
    string Title,
    string Secondary,
    Color TitleInk,
    Color SecondaryInk,
    TextFormat TitleStyle,
    TextFormat SecondaryStyle,
    Brush IconBrush,
    Brush TileBrush,
    ButtonPalette MorePalette,
    Action OnMore,
    AutomationInfo MoreAutomation)
{
    /// <summary>A bordered card: a clock tile, the title over its secondary line, and a trailing more button.</summary>
    public Ui Body => Border(
        Stack(
            Border(
                Icon.Fluent(FluentSymbol.Clock, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, IconBrush)
                    .HAlign(HorizontalAlignment.Center)
                    .VAlign(VerticalAlignment.Center))
                .Background(TileBrush)
                .CornerRadius(TimerVisuals.Radius.Control)
                .Width(TimerVisuals.IconSize.Tile)
                .Height(TimerVisuals.IconSize.Tile),
            Stack(
                Text(Title, TitleStyle, TitleInk),
                Text(Secondary, SecondaryStyle, SecondaryInk))
                .Spacing(2f)
                .VAlign(VerticalAlignment.Center)
                .Weight(1f),
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
            .Spacing(TimerVisuals.Spacing.Medium)
            .VAlign(VerticalAlignment.Center))
        .Background(Brush.Theme(ThemeColorToken.CardBackgroundFillColorDefault))
        .BorderBrush(Brush.Theme(ThemeColorToken.CardStrokeColorDefault))
        .BorderThickness(1f)
        .CornerRadius(TimerVisuals.Radius.Card)
        .Padding(TimerVisuals.Spacing.Medium);
}

/// <summary>
/// The virtualized, animated, Guid-keyed list of <see cref="HistoryRowView"/> cards for the selected folder, with a
/// localized empty-state caption revealed when the folder has no sessions. It re-supplies on every store or theme
/// revision (folder switch / add / move / delete / theme / language) with a fresh <see cref="HistoryRowData"/> list
/// keyed by <see cref="TimerEntry.Id"/>, so survivors rebind in place — re-theming rides the same-key rebind carried by
/// <see cref="HistoryRowData.Revision"/>, with <b>no</b> whole-list remount. The list is hosted directly (rather than
/// re-created behind a keyed boundary) so the nested repeater's list, scroll position, and keyboard focus identity are
/// preserved across revisions. The row template is supplied by the host (a method group on the owning control) so its
/// closures capture live state.
/// </summary>
/// <param name="Chrome">The plain command-and-theme surface (callbacks + live colours).</param>
/// <param name="Rows">The selected folder's history rows (entry + theme revision).</param>
/// <param name="BuildRow">Builds one history card (supplied by the host so it reads live theme state).</param>
/// <param name="FontFamily">The UI font family applied to the cards and empty caption.</param>
[Composite]
public readonly partial record struct HistoryListView(
    TimerChrome Chrome,
    IReadOnlyList<HistoryRowData> Rows,
    Func<HistoryRowData, HistoryRowView> BuildRow,
    string FontFamily)
{
    /// <summary>A scrolling, animated repeater of history cards over an empty-state caption.</summary>
    public Ui Body => Overlay(
        ScrollView(
            ItemsRepeater(
                Rows,
                static r => r.Entry.Id,
                BuildRow,
                new StackLayout { ItemExtent = TimerVisuals.Row.History, Spacing = TimerVisuals.Spacing.Small })
            .AnimateItems())
        .Vertical(ScrollMode.Auto)
        .HorizontalScrollBar(ScrollBarVisibility.Hidden),
        Text(
            Resources.NoHistory,
            new TextFormat(FontFamily, 14f, TextFormat.NormalWeight, TextAlignment.Center, TextWrapping.Wrap),
            Colors.TextFillColorSecondary)
            .MaxWidth(260f)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center)
            .Visible(Rows.Count == 0));

    internal ThemeColors Colors => Chrome.Colors;
}

/// <summary>
/// The history pane hosted in the inner split view's right pane — a section header and the Guid-keyed
/// <see cref="HistoryListView"/>.
/// </summary>
/// <remarks>
/// It is a <see cref="Control"/> (not a render-only value view) so the split view's <c>(Control, Control)</c> overload
/// wires its interaction and semantics trees — the history cards' more buttons are clickable, focusable, and
/// screen-reader reachable. It holds only the stable command-and-read <see cref="TimerChrome"/> surface and reads every
/// changing input (the history rows, the row template, the font, and the theme) live inside <see cref="Body"/>, so its
/// own reactive effect re-renders it in place. The list is hosted <b>directly</b> (not re-created behind a keyed
/// boundary) so the nested repeater's list, scroll position, and keyboard focus identity are preserved across revisions;
/// re-theming and re-localization ride the same-key rebind carried by <see cref="HistoryRowData.Revision"/>.
/// </remarks>
public sealed partial class HistoryPaneControl : Control
{
    private readonly TimerChrome _chrome;

    /// <summary>Creates the history pane over its command-and-read surface.</summary>
    /// <param name="chrome">The plain command-and-read surface (callbacks + live state + colours).</param>
    /// <exception cref="ArgumentNullException"><paramref name="chrome"/> is <see langword="null"/>.</exception>
    public HistoryPaneControl(TimerChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
    }

    /// <summary>The section header and the history list.</summary>
    public Ui Body => Stack(
        Text(Resources.History, new TextFormat(FontFamily, 18f, TextFormat.SemiBoldWeight), Colors.TextFillColorPrimary),
        new HistoryListView(_chrome, _chrome.HistoryRows, _chrome.BuildHistoryRow, FontFamily)
            .Weight(1f))
        .Spacing(TimerVisuals.Spacing.Medium)
        .Padding(TimerVisuals.Spacing.Large, TimerVisuals.Spacing.Large + TimerVisuals.Spacing.XSmall, TimerVisuals.Spacing.Large, TimerVisuals.Spacing.Large);

    private string FontFamily => _chrome.FontFamily;

    private ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }
}
