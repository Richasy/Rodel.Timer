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

/// <summary>The middle footer column, mounted only when the normal layout is wide enough AND the window material is in fallback.</summary>
[Composite]
public readonly partial record struct NormalFooterStatusView(
    string BackgroundStatus,
    ThemeColors Colors)
{
    /// <summary>The material-fallback caution line — the running / ready state now lives under the clock, so the footer only carries the (rare) material warning.</summary>
    public Ui Body => Stack(
        Text(
            BackgroundStatus,
            new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.NormalWeight, TextAlignment.Center),
            Colors.SystemFillColorCaution)
            .HAlign(HorizontalAlignment.Center));
}

/// <summary>The open-canvas timer stage: status, broken axis and clock, title editor, then primary action.</summary>
[Composite]
public readonly partial record struct NormalTimerStageView(
    TimerChrome Chrome,
    ThemeColors Colors,
    TimerNormalLayout Layout,
    TimeSpan Elapsed,
    bool IsRunning,
    string SessionTitle,
    string StatusCaption,
    string ActionLabel,
    bool AnimateClock)
{
    /// <summary>The centred stage with no enclosing card or opaque surface: a heavy clock over a fading axis, the caption below it, then the centred title editor and the primary action, the whole block vertically centred.</summary>
    public Ui Body => Overlay(
        Stack(
            Overlay(
                Rectangle(ClockGlowBrush(Colors, Chrome.IsDarkTheme), new Size(Layout.GlowWidth, Layout.GlowHeight))
                    .HAlign(HorizontalAlignment.Center)
                    .VAlign(VerticalAlignment.Center),
                Rectangle(AxisFadeBrush(Colors), new Size(Layout.AxisTotalWidth, 1f))
                    .HAlign(HorizontalAlignment.Center)
                    .VAlign(VerticalAlignment.Center)
                    .Visible(Layout.ShowAxisDecoration),
                When(
                    AnimateClock,
                    AnimatedText(TimerFormat.Clock(Elapsed), ClockStyle),
                    Text(TimerFormat.Clock(Elapsed), ClockStyle))
                    .HAlign(HorizontalAlignment.Center)
                    .VAlign(VerticalAlignment.Center))
                .Padding(0f, -(Layout.GlowHeight - (Layout.ClockSize * 1.5f)) / 2f),
            Text(
                StatusCaption,
                new TextFormat(TextFormat.DefaultFontFamily, 13f, TextFormat.NormalWeight, TextAlignment.Center),
                Colors.TextFillColorSecondary)
                .HAlign(HorizontalAlignment.Center),
            TextBox()
                .Text(SessionTitle)
                .OnTextChanged(Chrome.SetSessionTitle)
                .Placeholder(Resources.SessionTitlePlaceholder)
                .Centered()
                .FontSize(16f)
                .Padding(13f, 9f)
                .Automation(TitleAutomation)
                .MaxWidth(TimerVisuals.MaxWidth.SessionField)
                .HAlign(HorizontalAlignment.Center),
            Stack(
                Button(
                    Icon.Fluent(
                        IsRunning ? FluentSymbol.Stop : FluentSymbol.Play,
                        FluentIconVariant.Filled,
                        TimerVisuals.IconSize.Large),
                    Chrome.StartOrStop,
                    ButtonPalette.FromTheme(Colors, ButtonColorScheme.Accent))
                    .Style(PrimaryActionStyle)
                    .Width(TimerVisuals.Height.PrimaryAction)
                    .Height(TimerVisuals.Height.PrimaryAction)
                    .HAlign(HorizontalAlignment.Center)
                    .Automation(StartStopAutomation),
                Text(
                    ActionLabel,
                    new TextFormat(TextFormat.DefaultFontFamily, 12f, TextFormat.SemiBoldWeight),
                    Colors.TextFillColorSecondary)
                    .HAlign(HorizontalAlignment.Center))
                .Spacing(TimerVisuals.Spacing.XSmall)
                .HAlign(HorizontalAlignment.Center))
            .Spacing(TimerVisuals.Spacing.Medium)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center))
        .Padding(TimerVisuals.Spacing.XLarge, Layout.StageVerticalPadding);

    /// <summary>The circular primary action: zero padding + a radius that keeps a perfect circle at the 64 epx hero diameter.</summary>
    internal static ButtonStyle PrimaryActionStyle =>
        default(ButtonStyle).Padding(0f).CornerRadius(TimerVisuals.Height.PrimaryAction / 2f);

    /// <summary>The clock's block style; shared by the rolling and the still label so turning the animation off changes only the motion.</summary>
    internal TextFormat ClockStyle => new(
        TextFormat.DefaultFontFamily,
        Layout.ClockSize,
        TextFormat.BoldWeight,
        TextAlignment.Center);

    /// <summary>
    /// The fading axis rule line behind the clock: a bounds-relative horizontal gradient that fades in from
    /// transparent, holds the subtle divider colour, cuts to a transparent centre gap for the digits, then mirrors on
    /// the far side (the prototype''s <c>.timer-stage::before</c>). A relative gradient is used because the line''s
    /// laid-out bounds are unknown here in the composite.
    /// </summary>
    internal static Brush AxisFadeBrush(ThemeColors colors)
    {
        Color line = colors.DividerStrokeColorDefault;
        Color clear = line.MultiplyAlpha(0f);
        return Brush.CreateLinearRelative(
            new Point(0f, 0.5f),
            new Point(1f, 0.5f),
            [
                new GradientStop(0f, clear),
                new GradientStop(0.05f, line),
                new GradientStop(0.13f, line),
                new GradientStop(0.18f, clear),
                new GradientStop(0.82f, clear),
                new GradientStop(0.87f, line),
                new GradientStop(0.95f, line),
                new GradientStop(1f, clear),
            ]);
    }

    /// <summary>
    /// The soft radial backlight behind the clock: a bounds-relative accent glow fading from a bright centre through a
    /// long, gentle tail to transparent (the prototype''s blurred <c>.timer-stage::after</c>, approximated with a soft
    /// gradient falloff — a relative radial so it follows the backlight rectangle''s bounds without knowing them here).
    /// The falloff is theme-aware: light needs a much brighter core because an accent glow washes out against the pale
    /// canvas, while dark keeps a restrained core; both share the same long diffuse tail so the edge dissolves rather
    /// than reading as a hard-edged oval.
    /// </summary>
    internal static Brush ClockGlowBrush(ThemeColors colors, bool dark)
    {
        Color accent = colors.AccentFillColorDefault;
        float peak = dark ? 0.24f : 0.40f;
        return Brush.CreateRadialRelative(
            new Point(0.5f, 0.5f),
            0.5f,
            0.5f,
            [
                new GradientStop(0f, accent.MultiplyAlpha(peak)),
                new GradientStop(0.26f, accent.MultiplyAlpha(peak * 0.62f)),
                new GradientStop(0.56f, accent.MultiplyAlpha(peak * 0.24f)),
                new GradientStop(0.82f, accent.MultiplyAlpha(peak * 0.07f)),
                new GradientStop(1f, accent.MultiplyAlpha(0f)),
            ]);
    }

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

/// <summary>The footer's leading Folders command — present only in the side-panel layout, where the footer owns the entry points.</summary>
[Composite]
public readonly partial record struct FooterFoldersCommandView(
    TimerChrome Chrome,
    ThemeColors Colors)
{
    /// <summary>The icon-only command that slides the folders panel in from the leading edge.</summary>
    public Ui Body => Overlay(
        Button(
            Icon.Fluent(FluentSymbol.FolderList, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium),
            Chrome.ShowFoldersPanel,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .AccessKey("F")
            .ToolTip(Resources.Folders)
            .Automation(FoldersAutomation));

    internal static AutomationInfo FoldersAutomation => new()
    {
        Name = Resources.Folders,
        HelpText = Resources.FoldersHelp,
        AutomationId = TimerAutomationIds.FoldersPanelToggle,
        AccessKey = "F",
    };
}

/// <summary>The footer's trailing History command — present only in the side-panel layout.</summary>
[Composite]
public readonly partial record struct FooterHistoryCommandView(
    TimerChrome Chrome,
    ThemeColors Colors)
{
    /// <summary>The icon-only command that slides the history panel in from the trailing edge.</summary>
    public Ui Body => Overlay(
        Button(
            Icon.Fluent(FluentSymbol.History, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium),
            Chrome.ShowHistoryPanel,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .AccessKey("H")
            .ToolTip(Resources.History)
            .Automation(HistoryAutomation));

    internal static AutomationInfo HistoryAutomation => new()
    {
        Name = Resources.History,
        HelpText = Resources.HistoryHelp,
        AutomationId = TimerAutomationIds.HistoryPanelToggle,
        AccessKey = "H",
    };
}

/// <summary>The full-width normal footer: current folder, an optional material-fallback caution, and today's focus.</summary>
[Composite]
public readonly partial record struct NormalTimerFooterView(
    TimerChrome Chrome,
    ThemeColors Colors,
    TimerNormalLayout Layout,
    string FolderName,
    string FocusSummary,
    bool IsBackgroundFallback,
    string BackgroundStatus,
    string CurrentFolderLabel,
    string TodaysFocusLabel,
    bool ShowPanelCommands)
{
    /// <summary>
    /// The full-width bottom plane at a DEFINITE height. The middle column collapses to two columns unless a material
    /// fallback is showing, and the side-panel layout adds a command button beside each outer column — the footer is
    /// the only entry point to folders and history in that mode.
    /// </summary>
    public Ui Body => Stack(
        Divider.Horizontal(),
        Stack(
            Stack(
                When(ShowPanelCommands, new FooterFoldersCommandView(Chrome, Colors)),
                Stack(
                    Text(
                        CurrentFolderLabel,
                        new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.SemiBoldWeight),
                        Colors.TextFillColorSecondary),
                    Text(
                        FolderName,
                        new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight)))
                    .Spacing(2f)
                    .VAlign(VerticalAlignment.Center))
                .Orientation(Orientation.Horizontal)
                .Spacing(TimerVisuals.Spacing.Small)
                .VAlign(VerticalAlignment.Center)
                .Weight(1f),
            When(
                Layout.ShowMiddleStatus && IsBackgroundFallback,
                new NormalFooterStatusView(BackgroundStatus, Colors)),
            Stack(
                Stack(
                    Text(
                        TodaysFocusLabel,
                        new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.SemiBoldWeight, TextAlignment.Trailing),
                        Colors.TextFillColorSecondary)
                        .HAlign(HorizontalAlignment.Right),
                    Text(
                        FocusSummary,
                        new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight, TextAlignment.Trailing))
                        .HAlign(HorizontalAlignment.Right))
                    .Spacing(2f)
                    .VAlign(VerticalAlignment.Center)
                    .Weight(1f),
                When(ShowPanelCommands, new FooterHistoryCommandView(Chrome, Colors)))
                .Orientation(Orientation.Horizontal)
                .Spacing(TimerVisuals.Spacing.Small)
                .VAlign(VerticalAlignment.Center)
                .Weight(1f))
            .Orientation(Orientation.Horizontal)
            .Spacing(18f)
            .Padding(HorizontalPadding, Layout.FooterVerticalPadding)
            .Height(Layout.FooterHeight - TimerNormalLayoutResolver.FooterDividerHeight))
        .Spacing(0f)
        .Background(Brush.Theme(ThemeColorToken.LayerFillColorDefault));

    /// <summary>
    /// The footer's horizontal inset. With the panel commands present it drops to the side panels' own gutter, so each
    /// command button sits flush with the edge of the card it opens; without them the wider hero inset is kept.
    /// </summary>
    internal float HorizontalPadding => ShowPanelCommands
        ? TimerSidePanelLayoutResolver.Inset
        : TimerVisuals.Spacing.XXLarge;
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
    string BackgroundStatus,
    string CurrentFolderLabel,
    string TodaysFocusLabel,
    string StatusCaption,
    string ActionLabel,
    bool AnimateClock,
    bool ShowPanelCommands)
{
    /// <summary>The open timer stage and full-width footer, with no enclosing hero card.</summary>
    /// <remarks>
    /// The footer is <b>keyed</b> on the values it can only read at mount — a composite container's <c>.Padding</c> and
    /// a per-child <c>.Height</c> are applied when the subtree is built, not diffed — so switching the layout mode (or
    /// crossing the short-window breakpoint) remounts it with the right gutter and the right definite height. That
    /// height is what <see cref="TimerSidePanelLayoutResolver"/> measures the side panels' band against, so the two must
    /// never drift apart.
    /// </remarks>
    public Ui Body => Stack(
        new NormalTimerStageView(Chrome, Colors, Layout, Elapsed, IsRunning, SessionTitle, StatusCaption, ActionLabel, AnimateClock)
            .Weight(1f),
        Keyed(
            (ShowPanelCommands, Layout.FooterVerticalPadding),
            new NormalTimerFooterView(
                Chrome,
                Colors,
                Layout,
                FolderName,
                FocusSummary,
                IsBackgroundFallback,
                BackgroundStatus,
                CurrentFolderLabel,
                TodaysFocusLabel,
                ShowPanelCommands)));
}

/// <summary>The folder and running-state context that Tight removes from all three trees.</summary>
[Composite]
public readonly partial record struct CompactContextView(
    string FolderName,
    string StatusCaption,
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
            StatusCaption,
            new TextFormat(TextFormat.DefaultFontFamily, MathF.Max(11f, ClockSize * 0.3f), TextFormat.SemiBoldWeight)))
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
    bool IsRunning,
    string StatusCaption,
    bool AnimateClock)
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
            new CompactContextView(FolderName, StatusCaption, Colors, Layout.ClockSize))
            .Cell(0, 0),
        When(
            AnimateClock,
            AnimatedText(TimerFormat.Clock(Elapsed), ClockStyle),
            Text(TimerFormat.Clock(Elapsed), ClockStyle))
            .HAlign(HorizontalAlignment.Left)
            .VAlign(VerticalAlignment.Center)
            .Cell(0, ClockRow),
        Button(
            Icon.Fluent(
                IsRunning ? FluentSymbol.Stop : FluentSymbol.Play,
                FluentIconVariant.Filled,
                TimerVisuals.IconSize.Large),
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

    /// <summary>The compact clock's block style; shared by the rolling and the still label.</summary>
    internal TextFormat ClockStyle => new(
        TextFormat.DefaultFontFamily,
        Layout.ClockSize,
        TextFormat.SemiBoldWeight,
        TextAlignment.Leading);

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
    bool IsRunning,
    string StatusCaption,
    bool AnimateClock)
{
    /// <summary>The capped content band; the 32 epx title bar owns Exit and the native Close button.</summary>
    public Ui Body => Overlay(
        new CompactAdaptiveView(Chrome, Colors, Layout, FolderName, Elapsed, IsRunning, StatusCaption, AnimateClock)
            .VAlign(VerticalAlignment.Center))
        .Padding(Layout.OuterHorizontalPadding, 8f);
}
