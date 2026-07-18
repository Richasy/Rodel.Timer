using System.Collections.Generic;
using System.Linq;

using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Localization;
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
        SettingsCard(
            Resources.Theme,
            ComboBox([Resources.ThemeSystem, Resources.ThemeLight, Resources.ThemeDark])
                .SelectedIndex(_chrome.ThemeIndex)
                .OnSelectionChanged(_chrome.ThemeChanged)
                .FaceplateSizing(ComboBoxFaceplateSizing.SelectedItem)
                .Automation(SettingsContent.ThemeAutomation)
                .LayoutBox(minWidth: SettingsContent.SettingsEditorMinWidth, maxWidth: SettingsContent.SettingsEditorMaxWidth))
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
                .FaceplateSizing(ComboBoxFaceplateSizing.SelectedItem)
                .Automation(SettingsContent.BackgroundAutomation)
                .LayoutBox(minWidth: SettingsContent.SettingsEditorMinWidth, maxWidth: SettingsContent.SettingsEditorMaxWidth))
            .Description(Resources.WindowBackgroundDescription)
            .Icon(new FluentIcon(FluentSymbol.Color))
            .WrapThreshold(SettingsContent.CardWrapThreshold),
        SettingsCard(
            Resources.Font,
            ComboBox(_chrome.Fonts.Select(static font => font.DisplayName))
                .SelectedIndex(_chrome.FontIndex)
                .OnSelectionChanged(_chrome.FontChanged)
                .MaxDropDownHeight(320f)
                .FaceplateSizing(ComboBoxFaceplateSizing.SelectedItem)
                .Automation(SettingsContent.FontAutomation)
                .LayoutBox(minWidth: SettingsContent.SettingsEditorMinWidth, maxWidth: SettingsContent.SettingsEditorMaxWidth))
            .Description(Resources.FontDescription)
            .Icon(new FluentIcon(FluentSymbol.TextFont))
            .WrapThreshold(SettingsContent.CardWrapThreshold),
        SettingsCard(
            Resources.DailyTarget,
            NumberBox(_chrome.TargetMinutes, _chrome.TargetChanged)
                .Minimum(1d)
                .Maximum(TimerPreferences.MaximumDailyTargetMinutes)
                .SmallChange(30d)
                .LargeChange(60d)
                .AcceptsOnlyNumericInput()
                .Formatter(new DecimalNumberFormatter { MaxFractionDigits = 0 })
                .Automation(SettingsContent.DailyTargetAutomation)
                .LayoutBox(width: SettingsContent.SettingsNumberBoxWidth))
            .Description(Resources.DailyTargetDescription)
            .Icon(new FluentIcon(FluentSymbol.Target))
            .WrapThreshold(SettingsContent.CardWrapThreshold),
        SettingsCard(
            Resources.Language,
            ComboBox([Resources.LanguageSystem, Resources.LanguageEnglish, Resources.LanguageChinese, Resources.LanguageTraditionalChinese])
                .SelectedIndex(_chrome.LanguageIndex)
                .OnSelectionChanged(_chrome.LanguageChanged)
                .FaceplateSizing(ComboBoxFaceplateSizing.SelectedItem)
                .Automation(SettingsContent.LanguageAutomation)
                .LayoutBox(minWidth: SettingsContent.SettingsEditorMinWidth, maxWidth: SettingsContent.SettingsEditorMaxWidth))
            .Description(Resources.LanguageDescription)
            .Icon(new FluentIcon(FluentSymbol.LocalLanguage))
            .WrapThreshold(SettingsContent.CardWrapThreshold),
        Keyed(Localizer.CurrentLanguage, new AboutSettingsView(_chrome.Colors)))
        .Spacing(TimerVisuals.Spacing.Small);
}

/// <summary>
/// The About settings section: an expander (app icon + name, version at the tail) whose expanded row carries the app
/// description and a "open on GitHub" link. It is hosted through a <b>language-keyed remount</b> (see its call site)
/// because the generator hoists a nested composite's pure <c>Resources.X</c> reads to mount-time constants, so a
/// remount is what re-localizes the version / description / link text on a live language switch (the key is the
/// language STRING, so the remount reads the new language). The THEME colours no longer need a key: the framework's
/// <c>SettingsExpander</c> now reconciles its master content + item rows in place (Sprout ADR 0404), so a theme switch
/// re-colours the threaded palette without a remount.
/// </summary>
[Composite]
public readonly partial record struct AboutSettingsView(ThemeColors Colors)
{
    /// <summary>The About expander.</summary>
    public Ui Body => Overlay(
        SettingsExpander(Resources.AppName)
            .Icon(new ImageIcon(TimerAssets.AppLogoHandle()) { Size = new Size(20f, 20f) })
            .WrapThreshold(SettingsContent.CardWrapThreshold)
            .Content(Text(
                $"v{MainView.DisplayVersion}",
                new TextFormat(TextFormat.DefaultFontFamily, 13f, TextFormat.NormalWeight),
                Colors.TextFillColorSecondary))
            .Items(
                SettingsCard(
                    Resources.AppDescription,
                    HyperlinkButton(
                        Resources.OpenOnGitHub,
                        HyperlinkButtonPalette.FromTheme(Colors))
                        .OnClick(() => TimerLauncher.OpenUrl(TimerLinks.OpenSourceUri)))
                    .Automation(SettingsContent.OpenSourceAutomation)));
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

    // Every settings editor (the four combo boxes + the target NumberBox) sizes to its OWN content, bounded to a
    // shared [min, max] band so the column stays tidy. The combo boxes use FaceplateSizing.SelectedItem, so the closed
    // box hugs the CURRENT selection (not the widest option); the 120 min is the universal floor so a short editor is
    // never cramped, and the max only caps a very long current selection (the balloon is already gone) — generous
    // enough that a normal font name like "Microsoft YaHei UI" fits without trimming.
    internal const float SettingsEditorMinWidth = 120f;
    internal const float SettingsEditorMaxWidth = 300f;

    // The daily-target NumberBox is not a combo (no selected-item sizing) and stretches, so it gets its own tight,
    // fixed width — a number editor never needs the combo band.
    internal const float SettingsNumberBoxWidth = 150f;
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
        LanguageIndex = preferences.Language switch
        {
            TimerPreferences.EnglishLanguage => 1,
            TimerPreferences.ChineseLanguage => 2,
            TimerPreferences.TraditionalChineseLanguage => 3,
            _ => 0,
        };
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
        new SettingsSectionsControl(_sections)
            .Width(TimerVisuals.MaxWidth.Dialog));

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
        Name = Resources.AppDescription,
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
        string language = index switch
        {
            1 => TimerPreferences.EnglishLanguage,
            2 => TimerPreferences.ChineseLanguage,
            3 => TimerPreferences.TraditionalChineseLanguage,
            _ => TimerPreferences.SystemLanguage,
        };
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
