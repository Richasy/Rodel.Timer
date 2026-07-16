using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Rodel.Timer.Core.History;
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
/// The visual content of a history ListItem. The ItemsView container owns focus, all context gestures, and UIA; this
/// view draws only the clock tile and the session's title/detail.
/// </summary>
[Composite]
public readonly partial record struct HistoryRowView(
    string Title,
    string Secondary,
    Color TitleInk,
    Color SecondaryInk,
    TextFormat TitleStyle,
    TextFormat SecondaryStyle,
    Brush IconBrush,
    Brush TileBrush)
{
    /// <summary>The transparent history row content; the ItemContainer owns the 4 epx hover/selection chrome.</summary>
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
                .Weight(1f))
            .Orientation(Orientation.Horizontal)
            .Spacing(10f)
            .VAlign(VerticalAlignment.Center))
        .Padding(9f, 7f);
}

/// <summary>
/// An honest non-selecting history list: every stable Guid-keyed container is a focusable ListItem with x-of-y
/// semantics and a fresh owned context menu, but no fake Invoke or SelectionItem pattern.
/// </summary>
[Composite]
public readonly partial record struct HistoryListView(
    IReadOnlyList<HistoryRowData> Rows,
    Func<HistoryRowData, HistoryRowView> BuildRow,
    Func<HistoryRowData, AutomationInfo> BuildAutomation,
    Func<HistoryRowData, IFlyout> BuildContextMenu,
    ThemeColors Colors)
{
    /// <summary>The bounded history list over a localized empty state.</summary>
    public Ui Body => Overlay(
        BuildItemsView(),
        Text(
            Resources.NoHistory,
            new TextFormat(
                TextFormat.DefaultFontFamily,
                14f,
                TextFormat.NormalWeight,
                TextAlignment.Center,
                TextWrapping.Wrap),
            Colors.TextFillColorSecondary)
            .MaxWidth(260f)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center)
            .Visible(Rows.Count == 0));

    internal ItemsView BuildItemsView() =>
        TimerRowListFactory.History(
            Rows,
            BuildRow,
            new StackLayout { ItemExtent = TimerVisuals.Row.History, Spacing = TimerVisuals.Spacing.Small },
            BuildAutomation,
            BuildContextMenu);
}

/// <summary>
/// The fresh content host of the anchored History flyout. It keeps the inline ItemsView stable for this open lifetime
/// while same-key data/theme/language rebinds preserve each row's container, focus, scroll, and context identity.
/// </summary>
public sealed partial class HistoryPaneControl : Control
{
    private readonly TimerChrome _chrome;
    private readonly Func<HistoryRowData, AutomationInfo> _buildAutomation;
    private readonly Func<HistoryRowData, IFlyout> _buildContextMenu;

    /// <summary>Creates the History flyout content.</summary>
    /// <param name="chrome">The live read-and-command surface.</param>
    public HistoryPaneControl(TimerChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
        _buildAutomation = BuildAutomation;
        _buildContextMenu = BuildContextMenu;
    }

    /// <summary>The section header and history list.</summary>
    public Ui Body => Stack(
        Stack(
            Text(
                Resources.History,
                new TextFormat(TextFormat.DefaultFontFamily, 18f, TextFormat.SemiBoldWeight),
                Colors.TextFillColorPrimary),
            Text(
                HistorySubtitle,
                new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.NormalWeight),
                Colors.TextFillColorSecondary))
            .Spacing(2f),
        new HistoryListView(
            _chrome.HistoryRows,
            _chrome.BuildHistoryRow,
            _buildAutomation,
            _buildContextMenu,
            Colors)
            .Weight(1f))
        .Spacing(TimerVisuals.Spacing.Medium)
        .Padding(TimerVisuals.Spacing.Large);

    [SuppressMessage(
        "Performance",
        "CA1863:Use 'CompositeFormat'",
        Justification = "The format string is live-localized and must not be cached across a language switch.")]
    internal string HistorySubtitle =>
        string.Format(CultureInfo.CurrentCulture, Resources.HistoryFlyoutSubtitle, _chrome.SelectedFolderName);

    private ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }

    [SuppressMessage(
        "Performance",
        "CA1863:Use 'CompositeFormat'",
        Justification = "The format string is live-localized and must not be cached across a language switch.")]
    private static AutomationInfo BuildAutomation(HistoryRowData data)
    {
        TimerEntry entry = data.Entry;
        string duration = TimerFormat.Clock(TimeSpan.FromSeconds(entry.DurationSeconds));
        string created = entry.CreatedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
        string description = string.Format(
            CultureInfo.CurrentCulture,
            Resources.HistoryItemAccessibleDescription,
            entry.Title,
            duration,
            created);
        return new AutomationInfo
        {
            AutomationId = TimerAutomationIds.HistoryItem(entry.Id),
            HelpText = $"{description}. {Resources.HistoryRowHelp}",
            FullDescription = description,
        };
    }

    private MenuFlyout BuildContextMenu(HistoryRowData data) =>
        _chrome.BuildHistoryContextMenu(data.Entry.Id);
}
