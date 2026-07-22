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
                Relabel(_foldersFlyout, FoldersFlyoutAutomation),
                Relabel(_historyFlyout, HistoryFlyoutAutomation),
                Colors,
                Layout.ShowBrandLabel,
                Layout.ShowCommandLabels,
                _chrome.IsWindowPinned,
                _chrome.IsWindowMaximized,
                Resources.AppName),
            new CompactTimerTitleBarView(_chrome, Colors)));

    // Re-apply a stable anchored flyout's UIA pane Name (read fresh from the current language) each time this body is
    // evaluated, so a language switch — which re-runs the body — relabels the flyout on its next open. Flyout.Automation
    // just stores the info that is read at open time, and the flyout instances themselves are constructed once as fields.
    private static Flyout Relabel(Flyout flyout, AutomationInfo automation) => flyout.Automation(automation);

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
                TimerVisuals.IconSize.Small),
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
    bool ShowLabel)
{
    /// <summary>The glyph and optional label; the DropDownButton owns its decorative chevron.</summary>
    public Ui Body => Stack(
        Icon.Fluent(Symbol, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium)
            .VAlign(VerticalAlignment.Center),
        Text(
            Label,
            new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.NormalWeight))
            .VAlign(VerticalAlignment.Center)
            .Visible(ShowLabel))
        .Orientation(Orientation.Horizontal)
        .Spacing(6f);
}

/// <summary>The normal title bar's compact-overlay command, removed while the full window is pinned.</summary>
[Composite]
public readonly partial record struct NormalCompactOverlayButtonView(
    TimerChrome Chrome,
    ThemeColors Colors)
{
    /// <summary>The icon-only compact-overlay command.</summary>
    public Ui Body => Overlay(
        Button(
            Icon.Fluent(
                FluentSymbol.ContractUpRight,
                FluentIconVariant.Regular,
                TimerVisuals.IconSize.Medium),
            Chrome.ToggleCompact,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .ToolTip(Resources.CompactOverlay)
            .Automation(CompactAutomation));

    internal static AutomationInfo CompactAutomation => new()
    {
        Name = Resources.CompactOverlay,
        HelpText = Resources.CompactOverlayHelp,
        AutomationId = TimerAutomationIds.Compact,
    };
}

/// <summary>The controlled title-bar toggle for full-window always-on-top mode.</summary>
[Composite]
public readonly partial record struct WindowPinButtonView(
    TimerChrome Chrome,
    ThemeColors Colors,
    bool IsWindowPinned)
{
    /// <summary>The Pin/PinOff icon toggle with state-dependent tooltip and accessibility metadata.</summary>
    public Ui Body => Overlay(
        ToggleButton(
            Icon.Fluent(
                IsWindowPinned ? FluentSymbol.PinOff : FluentSymbol.Pin,
                IsWindowPinned ? FluentIconVariant.Filled : FluentIconVariant.Regular,
                TimerVisuals.IconSize.Medium),
            IsWindowPinned,
            TimerVisuals.GhostToggle(Colors))
            .OnToggled(Chrome.SetWindowPinned)
            .Style(TimerVisuals.ToggleButtons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .AccessKey("P")
            .ToolTip(IsWindowPinned ? Resources.UnpinWindow : Resources.PinWindow)
            .Automation(PinAutomation));

    internal AutomationInfo PinAutomation => new()
    {
        Name = IsWindowPinned ? Resources.UnpinWindow : Resources.PinWindow,
        HelpText = IsWindowPinned ? Resources.UnpinWindowHelp : Resources.PinWindowHelp,
        AutomationId = TimerAutomationIds.PinWindow,
        AccessKey = "P",
    };
}

/// <summary>
/// The extended title-bar row: brand and anchored Folders/History commands at the leading edge, an untouched drag
/// region, then compact/pin/settings commands before the system caption buttons.
/// </summary>
[Composite]
public readonly partial record struct TimerTitleBarView(
    TimerChrome Chrome,
    Flyout FoldersFlyout,
    Flyout HistoryFlyout,
    ThemeColors Colors,
    bool ShowBrandLabel,
    bool ShowCommandLabels,
    bool IsWindowPinned,
    bool IsWindowMaximized,
    string AppName)
{
    /// <summary>The responsive extended-title-bar content.</summary>
    public Ui Body => Stack(
        Stack(
            Icon.Image(TimerAssets.AppLogoHandle())
                .Width(TimerVisuals.IconSize.Medium)
                .Height(TimerVisuals.IconSize.Medium)
                .VAlign(VerticalAlignment.Center),
            Text(
                AppName,
                new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight))
                .VAlign(VerticalAlignment.Center)
                .Visible(ShowBrandLabel),
            DropDownButton(
                new TimerTitleBarDropDownContentView(
                    FluentSymbol.FolderList,
                    Resources.Folders,
                    ShowCommandLabels),
                FoldersFlyout)
                .Style(CommandStyle)
                .Palette(TimerVisuals.Ghost(Colors))
                .MinWidth(TimerVisuals.Height.Icon)
                .Height(TimerVisuals.Height.Icon)
                .VAlign(VerticalAlignment.Center)
                .AccessKey("F")
                .ToolTip(ShowCommandLabels ? null : Resources.Folders)
                .Automation(FoldersAutomation),
            DropDownButton(
                new TimerTitleBarDropDownContentView(
                    FluentSymbol.History,
                    Resources.History,
                    ShowCommandLabels),
                HistoryFlyout)
                .Style(CommandStyle)
                .Palette(TimerVisuals.Ghost(Colors))
                .MinWidth(TimerVisuals.Height.Icon)
                .Height(TimerVisuals.Height.Icon)
                .VAlign(VerticalAlignment.Center)
                .AccessKey("H")
                .ToolTip(ShowCommandLabels ? null : Resources.History)
                .Automation(HistoryAutomation))
            .Orientation(Orientation.Horizontal)
            .Spacing(TimerVisuals.Spacing.Small)
            .VAlign(VerticalAlignment.Center)
            .Weight(1f),
        When(
            WindowPinBehavior.ShowCompactOverlayButton,
            new NormalCompactOverlayButtonView(Chrome, Colors)),
        When(
            WindowPinBehavior.ShowPinButton,
            new WindowPinButtonView(Chrome, Colors, IsWindowPinned)),
        Button(
            Icon.Fluent(
                FluentSymbol.Settings,
                FluentIconVariant.Regular,
                TimerVisuals.IconSize.Medium),
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

    internal TimerWindowPinBehavior WindowPinBehavior =>
        TimerWindowPinPolicy.Resolve(IsWindowPinned, IsWindowMaximized);
}
