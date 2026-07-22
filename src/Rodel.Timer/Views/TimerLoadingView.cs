using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Theming;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The full-surface loading overlay shown while the workspace bootstrap is in flight — an indeterminate progress ring
/// over a localized "preparing data" caption on an opaque background. It is a render-only <c>[Composite]</c> value view
/// mounted for the shell's whole life and made visible only while <see cref="MainView"/> reports it is bootstrapping,
/// so it never churns the shell tree.
/// </summary>
/// <param name="Chrome">The plain command-and-theme surface (live colours).</param>
/// <param name="ThemeStamp">A monotonic stamp bumped on theme or language change to force a re-resolve.</param>
[Composite]
public readonly partial record struct TimerLoadingView(TimerChrome Chrome, int ThemeStamp)
{
    /// <summary>A centred progress ring over the "preparing data" caption on an opaque background.</summary>
    public Ui Body => Overlay(
        Stack(
            ProgressRing(new Size(48f, 48f))
                .Indeterminate()
                .Active(),
            Text(
                Resources.PreparingData,
                new TextFormat(TextFormat.DefaultFontFamily, 14f, TextFormat.NormalWeight),
                Colors.TextFillColorSecondary))
            .Spacing(16f)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center))
        .Background(Brush.Theme(ThemeColorToken.SolidBackgroundFillColorBase));

    internal ThemeColors Colors => Chrome.Colors;
}
