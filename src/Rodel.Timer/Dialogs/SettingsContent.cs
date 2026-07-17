using System.Collections.Generic;
using System.Linq;

using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer.Dialogs;

/// <summary>A plain live read-and-command surface for the interactive settings sections.</summary>
public sealed class SettingsSectionsChrome
{
    private readonly SettingsContent _owner;

    /// <summary>Creates the surface over its stable owner.</summary>
    public SettingsSectionsChrome(SettingsContent owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc/>
    public IReadOnlyList<FontChoice> Fonts => _owner.Fonts;
    /// <inheritdoc/>
    public int ThemeIndex => _owner.ThemeIndex;
    /// <inheritdoc/>
    public int BackgroundIndex => _owner.BackgroundIndex;
    /// <inheritdoc/>
    public int FontIndex => _owner.FontIndex;
    /// <inheritdoc/>
    public double TargetMinutes => _owner.TargetMinutes;
    /// <inheritdoc/>
    public int LanguageIndex => _owner.LanguageIndex;
    /// <inheritdoc/>
    public ThemeColors Colors => _owner.Colors;
    /// <inheritdoc/>
    public Action<int> ThemeChanged => _owner.OnThemeChanged;
    /// <inheritdoc/>
    public Action<int> BackgroundChanged => _owner.OnBackgroundChanged;
    /// <inheritdoc/>
    public Action<int> FontChanged => _owner.OnFontChanged;
    /// <inheritdoc/>
    public Action<NumberBoxValueChangedEventArgs> TargetChanged => _owner.OnTargetChanged;
    /// <inheritdoc/>
    public Action<int> LanguageChanged => _owner.OnLanguageChanged;
}

/// <summary>The interactive three-tree content hosted by the Settings ScrollView.</summary>
public sealed partial class SettingsSectionsControl : Control
{
    private readonly SettingsSectionsChrome _chrome;

    /// <summary>Creates the sections over their stable owner.</summary>
    public SettingsSectionsControl(SettingsSectionsChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
    }

    /// <summary>Appearance, Focus, Regional, and About groups in reading order.</summary>
    public Ui Body => Stack(
        Stack(
            Text(
                Resources.Appearance,
                new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight))
                .Foreground(Brush.Theme(ThemeColorToken.TextFillColorPrimary)),
            SettingsCard(
                Resources.Theme,
                ComboBox([Resources.ThemeSystem, Resources.ThemeLight, Resources.ThemeDark])
                    .SelectedIndex(_chrome.ThemeIndex)
                    .OnSelectionChanged(_chrome.ThemeChanged)
                    .Automation(SettingsContent.ThemeAutomation))
                .Description(Resources.ThemeDescription)
                .Icon(new FluentIcon(FluentSymbol.WeatherMoon))
                .WrapThreshold(SettingsContent.CardWrapThreshold),
            SettingsCard(
                Resources.WindowBackground,
                ComboBox(
                    [
                        Resources.BackgroundMica,
                        Resources.BackgroundMicaAlt,
                        Resources.BackgroundAcrylic,
                        Resources.BackgroundAcrylicThin,
                    ])
                    .SelectedIndex(_chrome.BackgroundIndex)
                    .OnSelectionChanged(_chrome.BackgroundChanged)
                    .Automation(SettingsContent.BackgroundAutomation))
                .Description(Resources.WindowBackgroundDescription)
                .Icon(new FluentIcon(FluentSymbol.Color))
                .WrapThreshold(SettingsContent.CardWrapThreshold),
            SettingsCard(
                Resources.Font,
                ComboBox(_chrome.Fonts.Select(static font => font.DisplayName))
                    .SelectedIndex(_chrome.FontIndex)
                    .OnSelectionChanged(_chrome.FontChanged)
                    .MaxDropDownHeight(320f)
                    .Automation(SettingsContent.FontAutomation))
                .Description(Resources.FontDescription)
                .Icon(new FluentIcon(FluentSymbol.TextFont))
                .WrapThreshold(SettingsContent.CardWrapThreshold))
        .Spacing(TimerVisuals.Spacing.Small),
        Stack(
            Text(
                Resources.Focus,
                new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight))
                .Foreground(Brush.Theme(ThemeColorToken.TextFillColorPrimary)),
            SettingsCard(
                Resources.DailyTarget,
                NumberBox(_chrome.TargetMinutes, _chrome.TargetChanged)
                    .Minimum(1d)
                    .Maximum(TimerPreferences.MaximumDailyTargetMinutes)
                    .SmallChange(30d)
                    .LargeChange(60d)
                    .AcceptsOnlyNumericInput()
                    .Automation(SettingsContent.DailyTargetAutomation))
                .Description(Resources.DailyTargetDescription)
                .Icon(new FluentIcon(FluentSymbol.Target))
                .WrapThreshold(SettingsContent.CardWrapThreshold))
        .Spacing(TimerVisuals.Spacing.Small),
        Stack(
            Text(
                Resources.Regional,
                new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight))
                .Foreground(Brush.Theme(ThemeColorToken.TextFillColorPrimary)),
            SettingsCard(
                Resources.Language,
                ComboBox([Resources.LanguageEnglish, Resources.LanguageChinese])
                    .SelectedIndex(_chrome.LanguageIndex)
                    .OnSelectionChanged(_chrome.LanguageChanged)
                    .Automation(SettingsContent.LanguageAutomation))
                .Description(Resources.LanguageDescription)
                .Icon(new FluentIcon(FluentSymbol.LocalLanguage))
                .WrapThreshold(SettingsContent.CardWrapThreshold))
        .Spacing(TimerVisuals.Spacing.Small),
        Stack(
            Text(
                Resources.About,
                new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight))
                .Foreground(Brush.Theme(ThemeColorToken.TextFillColorPrimary)),
            HyperlinkButton(Resources.OpenOnGitHub, TimerLinks.OpenSourceUri, HyperlinkButtonPalette.FromTheme(_chrome.Colors))
                .Automation(SettingsContent.OpenSourceAutomation))
        .Spacing(TimerVisuals.Spacing.Small))
        .Spacing(TimerVisuals.Spacing.Large);
}

/// <summary>
/// The live content of the settings dialog — grouped appearance, focus, regional, and about settings. Every change
/// applies immediately to the reactive store and to the window's theme, background, canonical font, or localizer,
/// so the product re-renders live behind the dialog. Selection indices are held as reactive state so the editors stay
/// consistent across a live language switch (which re-localizes their labels and options).
/// </summary>
public sealed partial class SettingsContent : Control
{
    private readonly MainView _main;
    private readonly IReadOnlyList<FontChoice> _fonts;
    private readonly Action? _onLanguageChanged;
    private readonly SettingsSectionsChrome _sections;

    // Keep each settings row in its horizontal layout (content on the right) at the dialog's ~500 epx content width:
    // the card's inner width there (500 - 2*16 padding = 468) sits just under SettingsCard's default 476 wrap
    // threshold, which would otherwise push the editor below the header. Lowering the threshold pins the mature
    // right-aligned layout while still wrapping if the dialog is ever made much narrower.
    internal const float CardWrapThreshold = 360f;
    /// <summary>Creates the settings content bound to the owning <see cref="MainView"/>.</summary>
    /// <param name="main">The state-owning root view (the store + window + fonts).</param>
    /// <param name="onLanguageChanged">
    /// Invoked after a live in-dialog language switch so the host can relabel the dialog's live-settable chrome — its
    /// title and command-button text — in the new language.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="main"/> is <see langword="null"/>.</exception>
    public SettingsContent(MainView main, Action? onLanguageChanged = null)
    {
        _main = main ?? throw new ArgumentNullException(nameof(main));
        _fonts = main.Fonts;
        _onLanguageChanged = onLanguageChanged;
        _sections = new SettingsSectionsChrome(this);

        TimerPreferences preferences = main.Preferences;
        ThemeIndex = (int)preferences.Theme;
        BackgroundIndex = (int)preferences.Background;
        LanguageIndex = preferences.Language == TimerPreferences.ChineseLanguage ? 1 : 0;
        FontIndex = ResolveFontIndex(preferences.FontFamily);
        TargetMinutes = preferences.DailyTargetMinutes;
    }

    /// <summary>
    /// The live theme colours resolved from the owning view's current theme. Reading <see cref="MainView.ThemeStamp"/>
    /// here subscribes this dialog's reactive body to the shell's theme/language stamp, so an in-dialog theme, font, or
    /// language switch re-renders the dialog with the live palette (rather than a palette frozen at construction).
    /// </summary>
    internal ThemeColors Colors
    {
        get
        {
            _ = _main.ThemeStamp;
            return _main.Window.Theme.Colors;
        }
    }

    /// <summary>The selected theme index (0 = system, 1 = light, 2 = dark).</summary>
    public partial int ThemeIndex { get; private set; }

    /// <summary>The selected background index, matching <see cref="TimerBackgroundMode"/>.</summary>
    public partial int BackgroundIndex { get; private set; }

    /// <summary>The selected language index (0 = English, 1 = Chinese).</summary>
    public partial int LanguageIndex { get; private set; }

    /// <summary>The selected UI font index into the offered fonts.</summary>
    public partial int FontIndex { get; private set; }

    /// <summary>The daily target in minutes.</summary>
    public partial double TargetMinutes { get; private set; }

    internal IReadOnlyList<FontChoice> Fonts => _fonts;

    /// <inheritdoc/>
    public Ui Body => Overlay(
        ScrollView(
            new SettingsSectionsControl(_sections))
        .Vertical(ScrollMode.Auto)
        .HorizontalScrollBar(ScrollBarVisibility.Hidden)
        .Width(TimerVisuals.MaxWidth.Dialog)
        .Height(500f));

    internal static AutomationInfo ThemeAutomation => new()
    {
        Name = Resources.Theme,
        AutomationId = TimerAutomationIds.SettingsTheme,
    };

    internal static AutomationInfo BackgroundAutomation => new()
    {
        Name = Resources.WindowBackground,
        AutomationId = TimerAutomationIds.SettingsBackground,
    };

    internal static AutomationInfo LanguageAutomation => new()
    {
        Name = Resources.Language,
        AutomationId = TimerAutomationIds.SettingsLanguage,
    };

    internal static AutomationInfo FontAutomation => new()
    {
        Name = Resources.Font,
        AutomationId = TimerAutomationIds.SettingsFont,
    };

    internal static AutomationInfo DailyTargetAutomation => new()
    {
        Name = Resources.DailyTarget,
        AutomationId = TimerAutomationIds.SettingsDailyTarget,
    };

    internal static AutomationInfo OpenSourceAutomation => new()
    {
        Name = Resources.OpenSource,
        HelpText = Resources.OpenSourceHelp,
        AutomationId = TimerAutomationIds.SettingsOpenSource,
    };

    private int ResolveFontIndex(string fontFamily)
    {
        for (int i = 0; i < _fonts.Count; i++)
        {
            if (string.Equals(_fonts[i].FamilyName, fontFamily, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return _fonts.Count > 0 ? 0 : -1;
    }

    internal void OnThemeChanged(int index)
    {
        ThemeIndex = index;
        _main.ApplyTheme((TimerThemeMode)index);
    }

    internal void OnBackgroundChanged(int index)
    {
        BackgroundIndex = index;
        _main.ApplyBackground((TimerBackgroundMode)index);
    }

    internal void OnLanguageChanged(int index)
    {
        LanguageIndex = index;
        string language = index == 1 ? TimerPreferences.ChineseLanguage : TimerPreferences.EnglishLanguage;
        _main.ApplyLanguage(language);
        _onLanguageChanged?.Invoke();
    }

    internal void OnFontChanged(int index)
    {
        if (index < 0 || index >= _fonts.Count)
        {
            return;
        }

        FontIndex = index;
        _main.ApplyFontFamily(_fonts[index].FamilyName);
    }

    internal void OnTargetChanged(NumberBoxValueChangedEventArgs args)
    {
        if (double.IsNaN(args.NewValue))
        {
            return;
        }

        int minutes = Math.Clamp((int)Math.Round(args.NewValue), 1, TimerPreferences.MaximumDailyTargetMinutes);
        TargetMinutes = minutes;
        _main.Store.UpdateDailyTarget(minutes);
    }
}
