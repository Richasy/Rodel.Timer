using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Theming;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The full-surface blocking-message overlay shown when the workspace fails to load or a save fails — a title, the
/// failure detail, and a single accent Retry button that re-drives the failed operation through
/// <see cref="MainView.Retry"/>. It is a render-only <c>[View]</c> value view that stays mounted for the shell's
/// life and is made visible only while there is a blocking message; the title and detail are supplied as record fields
/// so a bootstrap-failure or save-failure transition reconciles it in place.
/// </summary>
/// <param name="Chrome">The plain command-and-theme surface (retry callback + live colours).</param>
/// <param name="Title">The message title (save-failed vs load-error).</param>
/// <param name="Detail">The failure detail line.</param>
/// <param name="ThemeStamp">A monotonic stamp bumped on theme or language change to force a re-resolve.</param>
[View]
public readonly partial record struct TimerMessageView(
    TimerChrome Chrome,
    string Title,
    string Detail,
    int ThemeStamp)
{
    /// <summary>The centred title, detail, and accent Retry button on an opaque background.</summary>
    public Ui Body => Overlay([
        Column([
            Text(
               Title,
               new TextFormat(
                   TextFormat.DefaultFontFamily,
                   20f,
                   TextFormat.SemiBoldWeight,
                   TextAlignment.Center,
                   TextWrapping.Wrap)).MaxWidth(460f).HAlign(HorizontalAlignment.Center),
            Text(
                Detail,
                new TextFormat(
                    TextFormat.DefaultFontFamily,
                    14f,
                    TextFormat.NormalWeight,
                    TextAlignment.Center,
                    TextWrapping.Wrap),
                Colors.TextFillColorSecondary).MaxWidth(460f).HAlign(HorizontalAlignment.Center),
            Button(Resources.Retry, Chrome.Retry, ButtonPalette.FromTheme(Colors, ButtonColorScheme.Accent)).Automation(RetryAutomation).HAlign(HorizontalAlignment.Center)
        ]).Spacing(14f).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
    ]).Background(Brush.Theme(ThemeColorToken.SolidBackgroundFillColorBase)).Padding(24f);

    internal static AutomationInfo RetryAutomation => new()
    {
        Name = Resources.Retry,
        AutomationId = TimerAutomationIds.Retry,
    };

    internal ThemeColors Colors => Chrome.Colors;
}
