using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Styling;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>The middle footer column, mounted only when the normal layout has enough horizontal room.</summary>
[Composite]
public readonly partial record struct NormalFooterStatusView(
    bool IsRunning,
    bool ShowBackgroundStatus,
    string BackgroundStatus,
    ThemeColors Colors)
{
    /// <summary>The running state, with the material line appended only while the material is in fallback.</summary>
    public Ui Body => Stack(
        Stack(
            Rectangle(
                Brush.CreateSolid(IsRunning ? Colors.AccentFillColorDefault : Colors.SystemFillColorNeutral),
                new Size(6f, 6f))
                .Rounded(3f)
                .VAlign(VerticalAlignment.Center),
            Text(
                IsRunning ? Resources.TimerRunning : Resources.TimerReady,
                new TextFormat(TextFormat.DefaultFontFamily, 12f, TextFormat.SemiBoldWeight),
                Colors.TextFillColorSecondary))
            .Orientation(Orientation.Horizontal)
            .Spacing(6f)
            .HAlign(HorizontalAlignment.Center),
        Text(
            BackgroundStatus,
            new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.NormalWeight),
            Colors.SystemFillColorCaution)
            .HAlign(HorizontalAlignment.Center)
            .Visible(ShowBackgroundStatus))
        .Spacing(2f);
}

/// <summary>The open-canvas timer stage: status, broken axis and clock, title editor, then primary action.</summary>
[Composite]
public readonly partial record struct NormalTimerStageView(
    TimerChrome Chrome,
    ThemeColors Colors,
    TimerNormalLayout Layout,
    TimeSpan Elapsed,
    bool IsRunning,
    string SessionTitle)
{
    /// <summary>The centred stage with no enclosing card or opaque surface.</summary>
    public Ui Body => Stack(
        Text(
            IsRunning ? Resources.TimerRunning : Resources.TimerReady,
            new TextFormat(TextFormat.DefaultFontFamily, 13f, TextFormat.SemiBoldWeight, TextAlignment.Center),
            Colors.TextFillColorSecondary)
            .HAlign(HorizontalAlignment.Center),
        Stack(
            Rectangle(
                Brush.Theme(ThemeColorToken.DividerStrokeColorDefault),
                new Size(Layout.AxisSegmentWidth, 1f))
                .VAlign(VerticalAlignment.Center),
            AnimatedText(
                TimerFormat.Clock(Elapsed),
                new TextFormat(
                    TextFormat.DefaultFontFamily,
                    Layout.ClockSize,
                    TextFormat.SemiBoldWeight,
                    TextAlignment.Center),
                IsRunning ? Colors.AccentTextFillColorPrimary : Colors.TextFillColorPrimary)
                .HAlign(HorizontalAlignment.Center),
            Rectangle(
                Brush.Theme(ThemeColorToken.DividerStrokeColorDefault),
                new Size(Layout.AxisSegmentWidth, 1f))
                .VAlign(VerticalAlignment.Center))
            .Orientation(Orientation.Horizontal)
            .Spacing(TimerVisuals.Spacing.XLarge)
            .HAlign(HorizontalAlignment.Center),
        TextBox()
            .Text(SessionTitle)
            .OnTextChanged(Chrome.SetSessionTitle)
            .Placeholder(Resources.SessionTitlePlaceholder)
            .Automation(TitleAutomation)
            .MaxWidth(TimerVisuals.MaxWidth.HeroColumn)
            .HAlign(HorizontalAlignment.Center),
        Stack(
            Button(
                Icon.Fluent(
                    IsRunning ? FluentSymbol.Stop : FluentSymbol.Play,
                    FluentIconVariant.Filled,
                    TimerVisuals.IconSize.Large,
                    Brush.CreateSolid(Colors.TextOnAccentFillColorPrimary)),
                Chrome.StartOrStop,
                ButtonPalette.FromTheme(Colors, ButtonColorScheme.Accent))
                .Style(TimerVisuals.Buttons.CircularAction)
                .Width(TimerVisuals.Height.Hero)
                .Height(TimerVisuals.Height.Hero)
                .HAlign(HorizontalAlignment.Center)
                .Automation(StartStopAutomation),
            Text(
                IsRunning ? Resources.StopTimer : Resources.StartTimer,
                new TextFormat(TextFormat.DefaultFontFamily, 12f, TextFormat.SemiBoldWeight),
                Colors.TextFillColorSecondary)
                .HAlign(HorizontalAlignment.Center))
            .Spacing(TimerVisuals.Spacing.XSmall)
            .HAlign(HorizontalAlignment.Center))
        .Spacing(TimerVisuals.Spacing.Large)
        .Padding(TimerVisuals.Spacing.XLarge, Layout.StageVerticalPadding);

    internal static AutomationInfo TitleAutomation => new()
    {
        Name = Resources.SessionTitle,
        HelpText = Resources.SessionTitleHelp,
        AutomationId = TimerAutomationIds.Title,
    };

    internal AutomationInfo StartStopAutomation => new()
    {
        Name = IsRunning ? Resources.StopTimer : Resources.StartTimer,
        HelpText = IsRunning ? Resources.StopTimerHelp : Resources.StartTimerHelp,
        AutomationId = TimerAutomationIds.StartStop,
    };
}

/// <summary>The full-width normal footer: current folder, optional live status, and today's focus.</summary>
[Composite]
public readonly partial record struct NormalTimerFooterView(
    ThemeColors Colors,
    TimerNormalLayout Layout,
    string FolderName,
    bool IsRunning,
    string FocusSummary,
    bool IsBackgroundFallback,
    string BackgroundStatus)
{
    /// <summary>The full-width bottom plane; the middle status is structurally absent in the narrow state.</summary>
    public Ui Body => Border(
        Stack(
            Stack(
                Text(
                    Resources.CurrentFolder,
                    new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.SemiBoldWeight),
                    Colors.TextFillColorSecondary),
                Text(
                    FolderName,
                    new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight),
                    Colors.TextFillColorPrimary))
                .Spacing(2f)
                .VAlign(VerticalAlignment.Center)
                .Weight(1f),
            When(
                Layout.ShowMiddleStatus,
                new NormalFooterStatusView(IsRunning, IsBackgroundFallback, BackgroundStatus, Colors)),
            Stack(
                Text(
                    Resources.TodaysFocus,
                    new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.SemiBoldWeight, TextAlignment.Trailing),
                    Colors.TextFillColorSecondary)
                    .HAlign(HorizontalAlignment.Right),
                Text(
                    FocusSummary,
                    new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight, TextAlignment.Trailing),
                    Colors.TextFillColorPrimary)
                    .HAlign(HorizontalAlignment.Right))
                .Spacing(2f)
                .VAlign(VerticalAlignment.Center)
                .Weight(1f))
            .Orientation(Orientation.Horizontal)
            .Spacing(18f))
        .Background(Brush.Theme(ThemeColorToken.LayerFillColorDefault))
        .BorderBrush(Brush.Theme(ThemeColorToken.DividerStrokeColorDefault))
        .BorderThickness(1f)
        .Padding(TimerVisuals.Spacing.XXLarge, Layout.FooterVerticalPadding);
}

/// <summary>
/// The open normal workspace: a large rolling clock floats directly on the window canvas between two broken axis
/// segments, with the title and primary action on the same centre line and one full-width status footer at the bottom.
/// </summary>
[Composite]
public readonly partial record struct NormalTimerView(
    TimerChrome Chrome,
    ThemeColors Colors,
    TimerNormalLayout Layout,
    string FolderName,
    TimeSpan Elapsed,
    bool IsRunning,
    string SessionTitle,
    string FocusSummary,
    bool IsBackgroundFallback,
    string BackgroundStatus)
{
    /// <summary>The open timer stage and full-width footer, with no enclosing hero card.</summary>
    public Ui Body => Stack(
        new NormalTimerStageView(Chrome, Colors, Layout, Elapsed, IsRunning, SessionTitle)
            .Weight(1f),
        new NormalTimerFooterView(
            Colors,
            Layout,
            FolderName,
            IsRunning,
            FocusSummary,
            IsBackgroundFallback,
            BackgroundStatus));
}

/// <summary>The folder and running-state context that Tight removes from all three trees.</summary>
[Composite]
public readonly partial record struct CompactContextView(
    string FolderName,
    bool IsRunning,
    ThemeColors Colors,
    float ClockSize)
{
    /// <summary>The folder name over the timer state, aligned with the clock's leading edge.</summary>
    public Ui Body => Stack(
        Text(
            FolderName,
            new TextFormat(TextFormat.DefaultFontFamily, 9f, TextFormat.SemiBoldWeight),
            Colors.TextFillColorSecondary),
        Text(
            IsRunning ? Resources.TimerRunning : Resources.TimerReady,
            new TextFormat(TextFormat.DefaultFontFamily, MathF.Max(11f, ClockSize * 0.3f), TextFormat.SemiBoldWeight),
            Colors.TextFillColorPrimary))
        .Spacing(2f);
}

/// <summary>
/// One stable two-column compact grid. Track/cell metrics adapt between Tight, Balanced, and Wide while the primary
/// Button keeps identity; only the optional context subtree is structurally removed in Tight.
/// </summary>
[Composite]
public readonly partial record struct CompactAdaptiveView(
    TimerChrome Chrome,
    ThemeColors Colors,
    TimerCompactLayout Layout,
    string FolderName,
    TimeSpan Elapsed,
    bool IsRunning)
{
    internal static readonly GridLength[] Columns = [GridLength.Star(1f), GridLength.Auto];
    internal static readonly GridLength[] TightRows = [GridLength.Auto];
    internal static readonly GridLength[] DetailedRows = [GridLength.Auto, GridLength.Auto];

    /// <summary>The adaptive compact content grid.</summary>
    public Ui Body => Grid(
        Columns,
        RowTracks,
        When(
            Layout.ShowContext,
            new CompactContextView(FolderName, IsRunning, Colors, Layout.ClockSize))
            .Cell(0, 0),
        AnimatedText(
            TimerFormat.Clock(Elapsed),
            new TextFormat(
                TextFormat.DefaultFontFamily,
                Layout.ClockSize,
                TextFormat.SemiBoldWeight,
                TextAlignment.Leading),
            IsRunning ? Colors.AccentTextFillColorPrimary : Colors.TextFillColorPrimary)
            .HAlign(HorizontalAlignment.Left)
            .VAlign(VerticalAlignment.Center)
            .Cell(0, ClockRow),
        Button(
            Icon.Fluent(
                IsRunning ? FluentSymbol.Stop : FluentSymbol.Play,
                FluentIconVariant.Filled,
                TimerVisuals.IconSize.Large,
                Brush.CreateSolid(Colors.TextOnAccentFillColorPrimary)),
            Chrome.StartOrStop,
            ButtonPalette.FromTheme(Colors, ButtonColorScheme.Accent))
            .Style(ActionStyle)
            .Width(Layout.ActionSize)
            .Height(Layout.ActionSize)
            .HAlign(HorizontalAlignment.Right)
            .VAlign(VerticalAlignment.Center)
            .Cell(1, 0, rowSpan: ActionRowSpan)
            .Automation(StartStopAutomation))
        .ColumnSpacing(Layout.ColumnGap)
        .RowSpacing(2f)
        .Padding(Layout.HorizontalPadding, 0f);

    internal GridLength[] RowTracks =>
        Layout.ShowContext ? DetailedRows : TightRows;

    internal int ClockRow => Layout.ShowContext ? 1 : 0;

    internal int ActionRowSpan => Layout.ShowContext ? 2 : 1;

    internal ButtonStyle ActionStyle =>
        default(ButtonStyle)
            .Padding(0f)
            .CornerRadius(Layout.ActionSize / 2f);

    internal AutomationInfo StartStopAutomation => new()
    {
        Name = IsRunning ? Resources.StopTimer : Resources.StartTimer,
        HelpText = IsRunning ? Resources.StopTimerHelp : Resources.StartTimerHelp,
        AutomationId = TimerAutomationIds.StartStop,
    };
}

/// <summary>The compact-overlay content selected by the pure width+height resolver.</summary>
[Composite]
public readonly partial record struct CompactTimerView(
    TimerChrome Chrome,
    ThemeColors Colors,
    TimerCompactLayout Layout,
    string FolderName,
    TimeSpan Elapsed,
    bool IsRunning)
{
    /// <summary>The capped content band; the 32 epx title bar owns Exit and the native Close button.</summary>
    public Ui Body => Overlay(
        new CompactAdaptiveView(Chrome, Colors, Layout, FolderName, Elapsed, IsRunning)
            .VAlign(VerticalAlignment.Center))
        .Padding(Layout.OuterHorizontalPadding, 8f);
}
