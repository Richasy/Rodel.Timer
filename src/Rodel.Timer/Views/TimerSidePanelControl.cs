using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Animation;
using Sprout.Graphics;
using Sprout.Reconcile;
using Sprout.Theming;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The shared look of a side panel's card: the in-app acrylic fill, the flyout outline, the overlay corner radius, and
/// the flyout-elevation drop shadow, plus the timing of the slide.
/// </summary>
/// <remarks>
/// A side panel is hosted as a <b>chrome-less</b> popup precisely so the card can be drawn here: the framework's
/// <see cref="Sprout.Popups.PopupChrome"/> would draw its own surface and animate it with the flyout scale-fade, which
/// cannot express an edge-anchored slide. The popup foundation still supplies everything that is genuinely hard —
/// light dismiss, <c>Esc</c>, the focus trap and its restore, the accessibility landmark; only the look and the
/// transition are ours.
/// </remarks>
internal static class TimerSidePanelCard
{
    /// <summary>The card's corner radius — the framework overlay radius, so it reads as a sibling of the flyouts.</summary>
    internal const float CornerRadiusEpx = 8f;

    /// <summary>The slide's duration; the exit is held open for exactly this long before the popup is torn down.</summary>
    internal static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(250);

    /// <summary>The slide's implicit transform transition.</summary>
    internal static TransformTransition Slide { get; } = new(SlideDuration);

    /// <summary>The in-app acrylic fill for the current theme.</summary>
    /// <param name="dark">Whether the effective theme is dark.</param>
    /// <returns>The acrylic brush.</returns>
    internal static Brush Fill(bool dark) => Brush.CreateAcrylic(AcrylicMaterial.Default(dark));

    /// <summary>The WinUI flyout-elevation drop shadow (stronger in dark theme), drawn outside the card's own footprint.</summary>
    /// <param name="dark">Whether the effective theme is dark.</param>
    /// <returns>The drop shadow.</returns>
    internal static DropShadow Shadow(bool dark) =>
        new(new Color(0, 0, 0, dark ? (byte)66 : (byte)36), 6f, new System.Numerics.Vector2(0f, 8f));

    /// <summary>The card's resting translation: zero once shown, the layout's off-edge offset while hidden.</summary>
    /// <param name="layout">The resolved panel footprint.</param>
    /// <param name="isShown">Whether the panel has settled on screen.</param>
    /// <returns>The paint-time translation.</returns>
    internal static Point Translation(TimerSidePanelLayout layout, bool isShown) =>
        new(isShown ? 0f : layout.SlideOffset, 0f);
}

/// <summary>
/// The folders side panel: the ordinary <see cref="FolderPaneControl"/> inside the sliding acrylic card. The reveal
/// state lives on <see cref="MainView"/> (read here through the chrome) because the shell also owns the popup handle
/// and therefore the exit's timing — the card itself is a pure render host.
/// </summary>
public sealed partial class TimerFoldersPanelControl : Control
{
    private readonly TimerChrome _chrome;
    private readonly Action _close;

    /// <summary>Creates the folders panel.</summary>
    /// <param name="chrome">The live read-and-command surface.</param>
    /// <param name="close">Closes the panel after a folder is picked.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chrome"/> or <paramref name="close"/> is <see langword="null"/>.</exception>
    public TimerFoldersPanelControl(TimerChrome chrome, Action close)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
        _close = close ?? throw new ArgumentNullException(nameof(close));
    }

    /// <summary>The folder pane inside the translated, faded acrylic card.</summary>
    public Ui Body => Overlay(
        Transform(
            Opacity(
                Border(new FolderPaneControl(_chrome, _close))
                    .Background(TimerSidePanelCard.Fill(_chrome.IsDarkTheme))
                    .BorderBrush(Brush.CreateSolid(Colors.SurfaceStrokeColorFlyout))
                    .BorderThickness(1f)
                    .CornerRadius(TimerSidePanelCard.CornerRadiusEpx)
                    .Shadow(TimerSidePanelCard.Shadow(_chrome.IsDarkTheme))
                    .Width(Layout.Width)
                    .Height(Layout.Height))
                .Opacity(_chrome.IsSidePanelShown ? 1f : 0f)
                .OpacityTransition(OpacityTransition.Fast))
            .Translation(TimerSidePanelCard.Translation(Layout, _chrome.IsSidePanelShown))
            .TransformTransition(TimerSidePanelCard.Slide));

    private TimerSidePanelLayout Layout => _chrome.SidePanelLayout(TimerSidePanelSide.Left);

    private ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }
}

/// <summary>
/// The history side panel: the ordinary <see cref="HistoryPaneControl"/> inside the sliding acrylic card, mirrored to
/// the trailing edge.
/// </summary>
public sealed partial class TimerHistoryPanelControl : Control
{
    private readonly TimerChrome _chrome;

    /// <summary>Creates the history panel.</summary>
    /// <param name="chrome">The live read-and-command surface.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chrome"/> is <see langword="null"/>.</exception>
    public TimerHistoryPanelControl(TimerChrome chrome)
    {
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
    }

    /// <summary>The history pane inside the translated, faded acrylic card.</summary>
    public Ui Body => Overlay(
        Transform(
            Opacity(
                Border(new HistoryPaneControl(_chrome))
                    .Background(TimerSidePanelCard.Fill(_chrome.IsDarkTheme))
                    .BorderBrush(Brush.CreateSolid(Colors.SurfaceStrokeColorFlyout))
                    .BorderThickness(1f)
                    .CornerRadius(TimerSidePanelCard.CornerRadiusEpx)
                    .Shadow(TimerSidePanelCard.Shadow(_chrome.IsDarkTheme))
                    .Width(Layout.Width)
                    .Height(Layout.Height))
                .Opacity(_chrome.IsSidePanelShown ? 1f : 0f)
                .OpacityTransition(OpacityTransition.Fast))
            .Translation(TimerSidePanelCard.Translation(Layout, _chrome.IsSidePanelShown))
            .TransformTransition(TimerSidePanelCard.Slide));

    private TimerSidePanelLayout Layout => _chrome.SidePanelLayout(TimerSidePanelSide.Right);

    private ThemeColors Colors
    {
        get
        {
            _ = _chrome.ThemeStamp;
            return _chrome.Colors;
        }
    }
}
