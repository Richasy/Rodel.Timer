using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Popups;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Styling;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// Owns the extended-title-bar band and the two stable anchored Flyouts. The content factories build a fresh surface per
/// open, while each DropDownButton and Flyout controller keeps identity across responsive label changes.
/// </summary>
public sealed partial class TimerTitleBarControl : Control
{
    private static readonly FlyoutStyle AnchoredSurfaceStyle = default(FlyoutStyle)
        .Padding(0f)
        .MinWidth(TimerAnchoredSurfaceLayoutResolver.Width)
        .MaxWidth(TimerAnchoredSurfaceLayoutResolver.Width)
        .MaxHeight(TimerAnchoredSurfaceLayoutResolver.MaximumHeight);

    private readonly TimerChrome _chrome;
    private readonly Flyout _foldersFlyout;
    private readonly Flyout _historyFlyout;

    /// <summary>Creates the title-bar band and its stable anchored surfaces.</summary>
    /// <param name="chrome">The live read-and-command surface.</param>
    public TimerTitleBarControl(TimerChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));

        _foldersFlyout = new Flyout(close =>
            new FolderPaneControl(_chrome, close).Automation(FoldersContentAutomation))
        {
            Placement = PopupPlacement.BottomEdgeAlignedLeft,
            ShouldConstrainToRootBounds = true,
        }
            .Style(AnchoredSurfaceStyle)
            .Automation(FoldersFlyoutAutomation);

        _historyFlyout = new Flyout(_ =>
            new HistoryPaneControl(_chrome).Automation(HistoryContentAutomation))
        {
            Placement = PopupPlacement.BottomEdgeAlignedLeft,
            ShouldConstrainToRootBounds = true,
        }
            .Style(AnchoredSurfaceStyle)
            .Automation(HistoryFlyoutAutomation);
    }

    /// <summary>The normal title-bar band, removed entirely while the compact-overlay presenter is active.</summary>
    public Ui Body => Overlay(
        When(
            Layout.IsVisible,
            new TimerTitleBarView(
                _chrome,
                _foldersFlyout,
                _historyFlyout,
                Colors,
                Layout.ShowBrandLabel,
                Layout.ShowCommandLabels,
                Resources.AppName),
            new CompactTimerTitleBarView(_chrome, Colors)));

    private TimerTitleBarLayout Layout =>
        TimerTitleBarLayoutResolver.Resolve(_chrome.ViewportWidth, _chrome.IsCompact);

    private ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }

    private static AutomationInfo FoldersFlyoutAutomation => new()
    {
        Name = Resources.Folders,
        AutomationId = TimerAutomationIds.FoldersFlyout,
    };

    private static AutomationInfo FoldersContentAutomation => new()
    {
        Name = Resources.Folders,
        AutomationId = TimerAutomationIds.FoldersFlyoutContent,
    };

    private static AutomationInfo HistoryFlyoutAutomation => new()
    {
        Name = Resources.History,
        AutomationId = TimerAutomationIds.HistoryFlyout,
    };

    private static AutomationInfo HistoryContentAutomation => new()
    {
        Name = Resources.History,
        AutomationId = TimerAutomationIds.HistoryFlyoutContent,
    };
}

/// <summary>The compact title-bar's fixed leading Exit command; the remaining band stays draggable.</summary>
[Composite]
public readonly partial record struct CompactTimerTitleBarView(
    TimerChrome Chrome,
    ThemeColors Colors)
{
    /// <summary>Exit at the leading edge; the native compact presenter owns Close at the far edge.</summary>
    public Ui Body => Stack(
        Button(
            Icon.Fluent(
                FluentSymbol.ContractDownLeft,
                FluentIconVariant.Regular,
                TimerVisuals.IconSize.Small,
                Brush.CreateSolid(Colors.TextFillColorPrimary)),
            Chrome.ToggleCompact,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(28f)
            .Height(28f)
            .VAlign(VerticalAlignment.Center)
            .ToolTip(Resources.ExitCompactOverlay)
            .Automation(ExitAutomation))
        .Orientation(Orientation.Horizontal)
        .Padding(TimerVisuals.Spacing.XSmall, 0f);

    internal static AutomationInfo ExitAutomation => new()
    {
        Name = Resources.ExitCompactOverlay,
        HelpText = Resources.CompactOverlayHelp,
        AutomationId = TimerAutomationIds.Compact,
    };
}

/// <summary>The stable icon-plus-collapsible-label content of a title-bar DropDownButton.</summary>
[Composite]
public readonly partial record struct TimerTitleBarDropDownContentView(
    FluentSymbol Symbol,
    string Label,
    Brush IconBrush,
    Color LabelInk,
    bool ShowLabel)
{
    /// <summary>The glyph and optional label; the DropDownButton owns its decorative chevron.</summary>
    public Ui Body => Stack(
        Icon.Fluent(Symbol, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, IconBrush)
            .VAlign(VerticalAlignment.Center),
        Text(
            Label,
            new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.NormalWeight),
            LabelInk)
            .VAlign(VerticalAlignment.Center)
            .Visible(ShowLabel))
        .Orientation(Orientation.Horizontal)
        .Spacing(6f);
}

/// <summary>
/// The extended title-bar row: brand and anchored Folders/History commands at the leading edge, an untouched drag
/// region, then compact/settings commands before the system caption buttons.
/// </summary>
[Composite]
public readonly partial record struct TimerTitleBarView(
    TimerChrome Chrome,
    Flyout FoldersFlyout,
    Flyout HistoryFlyout,
    ThemeColors Colors,
    bool ShowBrandLabel,
    bool ShowCommandLabels,
    string AppName)
{
    /// <summary>The responsive extended-title-bar content.</summary>
    public Ui Body => Stack(
        Stack(
            Icon.Fluent(
                FluentSymbol.Timer,
                FluentIconVariant.Regular,
                TimerVisuals.IconSize.Medium,
                Brush.CreateSolid(Colors.AccentTextFillColorPrimary))
                .VAlign(VerticalAlignment.Center),
            Text(
                AppName,
                new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight),
                Colors.TextFillColorPrimary)
                .VAlign(VerticalAlignment.Center)
                .Visible(ShowBrandLabel),
            DropDownButton(
                new TimerTitleBarDropDownContentView(
                    FluentSymbol.FolderList,
                    Resources.Folders,
                    Brush.CreateSolid(Colors.TextFillColorPrimary),
                    Colors.TextFillColorPrimary,
                    ShowCommandLabels),
                FoldersFlyout)
                .Style(CommandStyle)
                .Palette(TimerVisuals.Ghost(Colors))
                .MinWidth(TimerVisuals.Height.Icon)
                .Height(TimerVisuals.Height.Icon)
                .VAlign(VerticalAlignment.Center)
                .AccessKey("F")
                .ToolTip(Resources.Folders)
                .Automation(FoldersAutomation),
            DropDownButton(
                new TimerTitleBarDropDownContentView(
                    FluentSymbol.History,
                    Resources.History,
                    Brush.CreateSolid(Colors.TextFillColorPrimary),
                    Colors.TextFillColorPrimary,
                    ShowCommandLabels),
                HistoryFlyout)
                .Style(CommandStyle)
                .Palette(TimerVisuals.Ghost(Colors))
                .MinWidth(TimerVisuals.Height.Icon)
                .Height(TimerVisuals.Height.Icon)
                .VAlign(VerticalAlignment.Center)
                .AccessKey("H")
                .ToolTip(Resources.History)
                .Automation(HistoryAutomation))
            .Orientation(Orientation.Horizontal)
            .Spacing(TimerVisuals.Spacing.Small)
            .VAlign(VerticalAlignment.Center)
            .Weight(1f),
        Button(
            Icon.Fluent(
                FluentSymbol.ContractUpRight,
                FluentIconVariant.Regular,
                TimerVisuals.IconSize.Medium,
                Brush.CreateSolid(Colors.TextFillColorPrimary)),
            Chrome.ToggleCompact,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .ToolTip(Resources.CompactOverlay)
            .Automation(CompactAutomation),
        Button(
            Icon.Fluent(
                FluentSymbol.Settings,
                FluentIconVariant.Regular,
                TimerVisuals.IconSize.Medium,
                Brush.CreateSolid(Colors.TextFillColorPrimary)),
            Chrome.ShowSettings,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .AccessKey("S")
            .ToolTip(Resources.Settings)
            .Automation(SettingsAutomation))
        .Orientation(Orientation.Horizontal)
        .Spacing(TimerVisuals.Spacing.Small)
        .Padding(TimerVisuals.Spacing.Medium, 0f, TimerVisuals.Spacing.Small, 0f);

    /// <summary>
    /// The title-bar command drop-down style. WinUI DropDownButtons size to their content, so the buttons carry only a
    /// <see cref="TimerVisuals.Height.Icon"/> minimum width (a fixed width clipped the icon + label + chevron), the native
    /// 4 epx control corner, and the control's own decorative chevron (shown only with the label). The zero horizontal
    /// padding in icon-only mode centres the lone glyph in the minimum-width square.
    /// </summary>
    internal DropDownButtonStyle CommandStyle =>
        default(DropDownButtonStyle)
            .Padding(ShowCommandLabels ? 11f : 0f, 5f)
            .CornerRadius(TimerVisuals.Radius.App)
            .ChevronVisible(ShowCommandLabels);

    internal static AutomationInfo FoldersAutomation => new()
    {
        Name = Resources.Folders,
        HelpText = Resources.FoldersHelp,
        AutomationId = TimerAutomationIds.FoldersToggle,
        AccessKey = "F",
    };

    internal static AutomationInfo HistoryAutomation => new()
    {
        Name = Resources.History,
        HelpText = Resources.HistoryHelp,
        AutomationId = TimerAutomationIds.HistoryToggle,
        AccessKey = "H",
    };

    internal static AutomationInfo SettingsAutomation => new()
    {
        Name = Resources.Settings,
        HelpText = Resources.SettingsHelp,
        AutomationId = TimerAutomationIds.Settings,
        AccessKey = "S",
    };

    internal static AutomationInfo CompactAutomation => new()
    {
        Name = Resources.CompactOverlay,
        HelpText = Resources.CompactOverlayHelp,
        AutomationId = TimerAutomationIds.Compact,
    };
}
