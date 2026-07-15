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
/// The full (non-compact) timer content — a central translucent hero card (folder label, rolling clock, session-title
/// input, and the round start/stop button) and a centred footer command dock (folder toggle · today summary · history
/// toggle). The brand, app name, settings, and compact command now live in the extended title bar
/// (<see cref="TimerTitleBarControl"/>), not here. It is a render-only <c>[Composite]</c> value view whose every
/// reactive input is supplied by the host <see cref="TimerContentControl"/> as a record field, so a tick, a theme
/// switch, or a title edit reconciles it in place.
/// </summary>
/// <remarks>
/// The view carries <b>four</b> interactive children (start/stop, the title box, the folder toggle, and the history
/// toggle), so the Group projects a bound per child — each toggle's accessibility bounds are its own control rect, not
/// the whole footer row. The outer surface paints <b>no</b> background so the window's Mica shows through; only the hero
/// card and the footer dock carry the translucent card token + a hairline stroke.
/// </remarks>
/// <param name="Chrome">The command surface (start-stop / pane toggles / title setter).</param>
/// <param name="Colors">The live theme palette resolved by the host.</param>
/// <param name="FontFamily">The UI font family applied to every label.</param>
/// <param name="FolderName">The selected folder's name shown above the clock.</param>
/// <param name="Elapsed">The running session's elapsed time (zero when idle).</param>
/// <param name="IsRunning">Whether a session is currently running (accent clock + stop affordance).</param>
/// <param name="SessionTitle">The in-progress session title being edited.</param>
/// <param name="SummaryText">The footer "today's total / target" summary line.</param>
[Composite]
public readonly partial record struct NormalTimerView(
    TimerChrome Chrome,
    ThemeColors Colors,
    string FontFamily,
    string FolderName,
    TimeSpan Elapsed,
    bool IsRunning,
    string SessionTitle,
    string SummaryText)
{
    /// <summary>The centred hero card and the footer command dock over a transparent surface.</summary>
    public Ui Body => Stack(
        Overlay(
            Border(
                Stack(
                    Text(
                        FolderName,
                        new TextFormat(FontFamily, 13f, TextFormat.NormalWeight, TextAlignment.Center),
                        Colors.TextFillColorSecondary)
                        .HAlign(HorizontalAlignment.Center),
                    AnimatedText(
                        TimerFormat.Clock(Elapsed),
                        new TextFormat(FontFamily, TimerVisuals.Clock.Hero, TextFormat.SemiBoldWeight, TextAlignment.Center),
                        IsRunning ? Colors.AccentTextFillColorPrimary : Colors.TextFillColorPrimary)
                        .HAlign(HorizontalAlignment.Center),
                    TextBox()
                        .Text(SessionTitle)
                        .OnTextChanged(Chrome.SetSessionTitle)
                        .Placeholder(Resources.SessionTitlePlaceholder)
                        .Automation(TitleAutomation)
                        .MaxWidth(TimerVisuals.MaxWidth.HeroColumn)
                        .HAlign(HorizontalAlignment.Center),
                    Button(
                        Icon.Fluent(IsRunning ? FluentSymbol.Stop : FluentSymbol.Play, FluentIconVariant.Filled, TimerVisuals.IconSize.Large, Brush.CreateSolid(Colors.TextOnAccentFillColorPrimary)),
                        Chrome.StartOrStop,
                        ButtonPalette.FromTheme(Colors, ButtonColorScheme.Accent))
                        .Style(TimerVisuals.Buttons.CircularAction)
                        .Width(TimerVisuals.Height.Hero)
                        .Height(TimerVisuals.Height.Hero)
                        .HAlign(HorizontalAlignment.Center)
                        .Automation(StartStopAutomation))
                    .Spacing(TimerVisuals.Spacing.Large)
                    .HAlign(HorizontalAlignment.Center))
                .Background(Brush.Theme(ThemeColorToken.CardBackgroundFillColorDefault))
                .BorderBrush(Brush.Theme(ThemeColorToken.CardStrokeColorDefault))
                .BorderThickness(1f)
                .CornerRadius(TimerVisuals.Radius.Hero)
                .Padding(TimerVisuals.Spacing.XXLarge, TimerVisuals.Spacing.XLarge, TimerVisuals.Spacing.XXLarge, TimerVisuals.Spacing.XLarge)
                .MaxWidth(TimerVisuals.MaxWidth.Hero)
                .HAlign(HorizontalAlignment.Center)
                .VAlign(VerticalAlignment.Center))
            .Weight(1f),
        Overlay(
            Border(
                Stack(
                    Button(
                        Icon.Fluent(FluentSymbol.FolderList, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, Brush.CreateSolid(Colors.TextFillColorPrimary)),
                        Chrome.ToggleFolderPane,
                        TimerVisuals.Ghost(Colors))
                        .Style(TimerVisuals.Buttons.Icon)
                        .Width(TimerVisuals.Height.Standard)
                        .Height(TimerVisuals.Height.Standard)
                        .Automation(FoldersAutomation),
                    Text(
                        SummaryText,
                        new TextFormat(FontFamily, 13f, TextFormat.NormalWeight),
                        Colors.TextFillColorSecondary)
                        .VAlign(VerticalAlignment.Center),
                    Button(
                        Icon.Fluent(FluentSymbol.History, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, Brush.CreateSolid(Colors.TextFillColorPrimary)),
                        Chrome.ToggleHistoryPane,
                        TimerVisuals.Ghost(Colors))
                        .Style(TimerVisuals.Buttons.Icon)
                        .Width(TimerVisuals.Height.Standard)
                        .Height(TimerVisuals.Height.Standard)
                        .Automation(HistoryAutomation))
                    .Orientation(Orientation.Horizontal)
                    .Spacing(TimerVisuals.Spacing.Medium)
                    .VAlign(VerticalAlignment.Center))
                .Background(Brush.Theme(ThemeColorToken.CardBackgroundFillColorDefault))
                .BorderBrush(Brush.Theme(ThemeColorToken.CardStrokeColorDefault))
                .BorderThickness(1f)
                .CornerRadius(TimerVisuals.Radius.Card)
                .Padding(TimerVisuals.Spacing.XSmall + 2f)
                .HAlign(HorizontalAlignment.Center))
            .HAlign(HorizontalAlignment.Center))
        .Spacing(TimerVisuals.Spacing.Small)
        .Padding(TimerVisuals.Spacing.Large, TimerVisuals.Spacing.Medium, TimerVisuals.Spacing.Large, TimerVisuals.Spacing.Large);

    internal static AutomationInfo TitleAutomation => new()
    {
        Name = Resources.SessionTitle,
        HelpText = Resources.SessionTitleHelp,
        AutomationId = TimerAutomationIds.Title,
    };

    internal static AutomationInfo FoldersAutomation => new()
    {
        Name = Resources.Folders,
        HelpText = Resources.FoldersHelp,
        AutomationId = TimerAutomationIds.FoldersToggle,
    };

    internal static AutomationInfo HistoryAutomation => new()
    {
        Name = Resources.History,
        HelpText = Resources.HistoryHelp,
        AutomationId = TimerAutomationIds.HistoryToggle,
    };

    internal AutomationInfo StartStopAutomation => new()
    {
        Name = IsRunning ? Resources.StopTimer : Resources.StartTimer,
        HelpText = IsRunning ? Resources.StopTimerHelp : Resources.StartTimerHelp,
        AutomationId = TimerAutomationIds.StartStop,
    };
}

/// <summary>
/// The compact-overlay timer content — only the rolling clock, the round start/stop button, and an exit-compact chip,
/// laid out for the small 360×203 overlay presenter (no header, title, or footer). It is a render-only
/// <c>[Composite]</c> value view; toggling the compact overlay swaps the whole <see cref="NormalTimerView"/> for this
/// one through the host's <c>When(...)</c>, so the header / title / footer nodes are torn out of all three trees while
/// compact and return on exit.
/// </summary>
/// <param name="Chrome">The command surface (start-stop + exit-compact).</param>
/// <param name="Colors">The live theme palette resolved by the host.</param>
/// <param name="FontFamily">The UI font family applied to the clock.</param>
/// <param name="Elapsed">The running session's elapsed time (zero when idle).</param>
/// <param name="IsRunning">Whether a session is currently running (accent clock + stop affordance).</param>
[Composite]
public readonly partial record struct CompactTimerView(
    TimerChrome Chrome,
    ThemeColors Colors,
    string FontFamily,
    TimeSpan Elapsed,
    bool IsRunning)
{
    /// <summary>The clock, the round start/stop button, and the exit-compact chip, centred for the overlay.</summary>
    public Ui Body => Overlay(
        Stack(
            AnimatedText(
                TimerFormat.Clock(Elapsed),
                new TextFormat(FontFamily, TimerVisuals.Clock.Compact, TextFormat.SemiBoldWeight, TextAlignment.Center),
                IsRunning ? Colors.AccentTextFillColorPrimary : Colors.TextFillColorPrimary)
                .HAlign(HorizontalAlignment.Center),
            Button(
                Icon.Fluent(IsRunning ? FluentSymbol.Stop : FluentSymbol.Play, FluentIconVariant.Filled, TimerVisuals.IconSize.Large, Brush.CreateSolid(Colors.TextOnAccentFillColorPrimary)),
                Chrome.StartOrStop,
                ButtonPalette.FromTheme(Colors, ButtonColorScheme.Accent))
                .Style(TimerVisuals.Buttons.CompactCircularAction)
                .Width(TimerVisuals.Height.CompactStart)
                .Height(TimerVisuals.Height.CompactStart)
                .HAlign(HorizontalAlignment.Center)
                .Automation(StartStopAutomation),
            Button(
                Icon.Fluent(FluentSymbol.PictureInPictureExit, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, Brush.CreateSolid(Colors.TextFillColorPrimary)),
                Chrome.ToggleCompact,
                TimerVisuals.Ghost(Colors))
                .Style(TimerVisuals.Buttons.Icon)
                .Width(TimerVisuals.Height.Icon)
                .Height(TimerVisuals.Height.Icon)
                .HAlign(HorizontalAlignment.Center)
                .Automation(ExitCompactAutomation))
            .Spacing(TimerVisuals.Spacing.Small)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center))
        .Padding(TimerVisuals.Spacing.Medium);

    internal static AutomationInfo ExitCompactAutomation => new()
    {
        Name = Resources.ExitCompactOverlay,
        HelpText = Resources.CompactOverlayHelp,
        AutomationId = TimerAutomationIds.Compact,
    };

    internal AutomationInfo StartStopAutomation => new()
    {
        Name = IsRunning ? Resources.StopTimer : Resources.StartTimer,
        HelpText = IsRunning ? Resources.StopTimerHelp : Resources.StartTimerHelp,
        AutomationId = TimerAutomationIds.StartStop,
    };
}
