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

/// <summary>
/// The live content of the settings dialog — theme, language, UI font, and daily-target editors, plus an open-source
/// link. Every change applies immediately to the reactive store (and, for theme/language, to the window and localizer),
/// so the product re-renders live behind the dialog. Selection indices are held as reactive state so the editors stay
/// consistent across a live language switch (which re-localizes their labels and options).
/// </summary>
public sealed partial class SettingsContent : Control
{
    private readonly MainView _main;
    private readonly IReadOnlyList<FontChoice> _fonts;
    private readonly Action? _onLanguageChanged;

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

        TimerPreferences preferences = main.Preferences;
        ThemeIndex = (int)preferences.Theme;
        LanguageIndex = preferences.Language == TimerPreferences.ChineseLanguage ? 1 : 0;
        FontIndex = ResolveFontIndex(preferences.FontFamily);
        TargetMinutes = preferences.DailyTargetMinutes;
    }

    /// <summary>
    /// The live theme colours resolved from the owning view's current theme. Reading <see cref="MainView.ThemeStamp"/>
    /// here subscribes this dialog's reactive body to the shell's theme/language stamp, so an in-dialog theme, font, or
    /// language switch re-renders the dialog with the live palette (rather than a palette frozen at construction).
    /// </summary>
    private ThemeColors Colors
    {
        get
        {
            _ = _main.ThemeStamp;
            return _main.Window.Theme.Colors;
        }
    }

    /// <summary>The selected theme index (0 = system, 1 = light, 2 = dark).</summary>
    public partial int ThemeIndex { get; private set; }

    /// <summary>The selected language index (0 = English, 1 = Chinese).</summary>
    public partial int LanguageIndex { get; private set; }

    /// <summary>The selected UI font index into the offered fonts.</summary>
    public partial int FontIndex { get; private set; }

    /// <summary>The daily target in minutes.</summary>
    public partial double TargetMinutes { get; private set; }

    /// <inheritdoc/>
    public Ui Body => Overlay(
        Stack(
            SettingsCard(
                Resources.Theme,
                ComboBox([Resources.ThemeSystem, Resources.ThemeLight, Resources.ThemeDark])
                    .SelectedIndex(ThemeIndex)
                    .OnSelectionChanged(OnThemeChanged)
                    .Automation(ThemeAutomation))
                .Description(Resources.ThemeDescription)
                .Icon(new FluentIcon(FluentSymbol.WeatherMoon))
                .WrapThreshold(CardWrapThreshold),
            SettingsCard(
                Resources.Language,
                ComboBox([Resources.LanguageEnglish, Resources.LanguageChinese])
                    .SelectedIndex(LanguageIndex)
                    .OnSelectionChanged(OnLanguageChanged)
                    .Automation(LanguageAutomation))
                .Description(Resources.LanguageDescription)
                .Icon(new FluentIcon(FluentSymbol.LocalLanguage))
                .WrapThreshold(CardWrapThreshold),
            SettingsCard(
                Resources.Font,
                ComboBox(_fonts.Select(f => f.DisplayName))
                    .SelectedIndex(FontIndex)
                    .OnSelectionChanged(OnFontChanged)
                    .MaxDropDownHeight(320f)
                    .Automation(FontAutomation))
                .Description(Resources.FontDescription)
                .Icon(new FluentIcon(FluentSymbol.TextFont))
                .WrapThreshold(CardWrapThreshold),
            SettingsCard(
                Resources.DailyTarget,
                NumberBox(TargetMinutes, OnTargetChanged)
                    .Minimum(1d)
                    .Maximum(TimerPreferences.MaximumDailyTargetMinutes)
                    .SmallChange(30d)
                    .LargeChange(60d)
                    .AcceptsOnlyNumericInput()
                    .Automation(DailyTargetAutomation))
                .Description(Resources.DailyTargetDescription)
                .Icon(new FluentIcon(FluentSymbol.Target))
                .WrapThreshold(CardWrapThreshold),
            HyperlinkButton(Resources.OpenOnGitHub, TimerLinks.OpenSourceUri, HyperlinkButtonPalette.FromTheme(Colors))
                .Automation(OpenSourceAutomation))
        .Spacing(TimerVisuals.Spacing.Small)
        .Width(TimerVisuals.MaxWidth.Dialog));

    internal static AutomationInfo ThemeAutomation => new()
    {
        Name = Resources.Theme,
        AutomationId = TimerAutomationIds.SettingsTheme,
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

    private void OnThemeChanged(int index)
    {
        ThemeIndex = index;
        var mode = (TimerThemeMode)index;
        _main.Store.UpdateTheme(mode);
        _main.Window.RequestedTheme = mode switch
        {
            TimerThemeMode.Light => ThemeRequest.Light,
            TimerThemeMode.Dark => ThemeRequest.Dark,
            _ => null,
        };
        _main.NotifyThemeChanged();
    }

    private void OnLanguageChanged(int index)
    {
        LanguageIndex = index;
        string language = index == 1 ? TimerPreferences.ChineseLanguage : TimerPreferences.EnglishLanguage;
        _main.ApplyLanguage(language);
        _onLanguageChanged?.Invoke();
    }

    private void OnFontChanged(int index)
    {
        if (index < 0 || index >= _fonts.Count)
        {
            return;
        }

        FontIndex = index;
        _main.Store.UpdateFontFamily(_fonts[index].FamilyName);
        _main.NotifyThemeChanged();
    }

    private void OnTargetChanged(NumberBoxValueChangedEventArgs args)
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
