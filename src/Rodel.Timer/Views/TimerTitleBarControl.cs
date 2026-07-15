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
/// The window title-bar band content — hosted with <see cref="Window.SetTitleBarContent(Control)"/> so it draws in the
/// extended caption band (left of the system caption buttons) and its two command buttons are carved out of the drag
/// region automatically. It resolves the live theme and language from the shared <see cref="TimerChrome"/> surface, then
/// branches on the compact-overlay flag: while normal it shows the reactive <see cref="TimerTitleBarView"/> (brand glyph
/// + app name + compact + settings buttons); while compact the whole band is torn out of all three trees by a
/// <c>When(!IsCompact, …)</c> switch, so the compact presenter's collapsed 32 epx band exposes no interactive node.
/// </summary>
/// <remarks>
/// A dedicated <see cref="Control"/> is the correct shape here (not a superfluous composite): it is the
/// <see cref="Window.SetTitleBarContent(Control)"/> three-tree / drag carve-out boundary, and it owns the reactive
/// theme-resolve effect. Reading each reactive value here (rather than inside the branch view) subscribes this control's
/// effect, so a theme switch, a language change, or entering / leaving compact re-renders the band in place. The band
/// carries <b>two</b> interactive children (compact + settings), so the projected Group gives each button its own hit
/// bounds — the "single settings node inherits the whole caption band" bug cannot recur.
/// </remarks>
public sealed partial class TimerTitleBarControl : Control
{
    private readonly TimerChrome _chrome;

    /// <summary>Creates the title-bar band over its command-and-read surface.</summary>
    /// <param name="chrome">The plain command-and-read surface (callbacks + live state + colours).</param>
    /// <exception cref="ArgumentNullException"><paramref name="chrome"/> is <see langword="null"/>.</exception>
    public TimerTitleBarControl(TimerChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
    }

    /// <summary>The brand + command band while normal; nothing while the compact overlay is active.</summary>
    public Ui Body => Overlay(
        When(
            !_chrome.IsCompact,
            new TimerTitleBarView(
                _chrome,
                Colors,
                _chrome.FontFamily,
                Resources.AppName)));

    private Sprout.Theming.ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }
}

/// <summary>
/// The extended title-bar row — a leading brand glyph, the product name, a weighted drag spacer, and the trailing
/// compact + settings icon buttons (each 36×36, radius 8, ghost palette), laid out left of the system caption buttons.
/// It is a render-only <c>[Composite]</c> value view whose reactive inputs are supplied by the host
/// <see cref="TimerTitleBarControl"/> as record fields, so a theme switch or a language change reconciles it in place.
/// </summary>
/// <remarks>
/// The empty band area (the brand glyph, the app-name label, and the weighted gap) carries no interaction node, so it
/// stays part of the window drag region; only the two buttons carve out interaction leaves. The row paints no
/// background so the window's Mica / theme band shows through.
/// </remarks>
/// <param name="Chrome">The command surface (settings + compact toggle).</param>
/// <param name="Colors">The live theme palette resolved by the host.</param>
/// <param name="FontFamily">The UI font family applied to the app name.</param>
/// <param name="AppName">The product name shown beside the brand glyph.</param>
[Composite]
public readonly partial record struct TimerTitleBarView(
    TimerChrome Chrome,
    ThemeColors Colors,
    string FontFamily,
    string AppName)
{
    /// <summary>The brand glyph, the app name, a weighted drag spacer, and the compact + settings buttons.</summary>
    public Ui Body => Stack(
        Icon.Fluent(FluentSymbol.Timer, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, Brush.CreateSolid(Colors.AccentTextFillColorPrimary))
            .VAlign(VerticalAlignment.Center),
        Text(
            AppName,
            new TextFormat(FontFamily, 14f, TextFormat.SemiBoldWeight),
            Colors.TextFillColorPrimary)
            .VAlign(VerticalAlignment.Center)
            .Weight(1f),
        Button(
            Icon.Fluent(FluentSymbol.PictureInPictureEnter, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, Brush.CreateSolid(Colors.TextFillColorPrimary)),
            Chrome.ToggleCompact,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .Automation(CompactAutomation),
        Button(
            Icon.Fluent(FluentSymbol.Settings, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, Brush.CreateSolid(Colors.TextFillColorPrimary)),
            Chrome.ShowSettings,
            TimerVisuals.Ghost(Colors))
            .Style(TimerVisuals.Buttons.Icon)
            .Width(TimerVisuals.Height.Icon)
            .Height(TimerVisuals.Height.Icon)
            .VAlign(VerticalAlignment.Center)
            .AccessKey("S")
            .Automation(SettingsAutomation))
        .Orientation(Orientation.Horizontal)
        .Spacing(TimerVisuals.Spacing.Small)
        .Padding(TimerVisuals.Spacing.Medium, 0f, TimerVisuals.Spacing.Small, 0f);

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
