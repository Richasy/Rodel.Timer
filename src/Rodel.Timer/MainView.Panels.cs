using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Graphics;
using Sprout.Popups;
using Sprout.Semantics;

namespace Rodel.Timer;

/// <summary>
/// The side-panel surface of <see cref="MainView"/>: the footer-launched Folders and History panels of the
/// <see cref="Core.Settings.TimerLayoutMode.SidePanel"/> layout.
/// </summary>
/// <remarks>
/// Each panel is a <b>chrome-less, light-dismiss popup</b> whose content draws its own acrylic card and plays its own
/// edge-anchored slide. Hosting it on the popup foundation — rather than as another overlay inside the content tree —
/// is what gives it click-outside and <c>Esc</c> dismissal, a focus trap with a focus restore back to the launching
/// command, and a named accessibility landmark, none of which a bare overlay would have. Only one panel is open at a
/// time; the shell owns that invariant because the two panels are alternatives, not a stack.
/// </remarks>
public sealed partial class MainView
{
    private static readonly TimeSpan FrameDelay = TimeSpan.FromMilliseconds(16);

    private PopupHandle? _panelHandle;
    private DispatcherQueueTimer? _panelTimer;
    private bool _panelExiting;

    /// <summary>
    /// Whether the open panel has settled on screen. The card mounts with this <see langword="false"/> (translated off
    /// its edge, transparent) and it is flipped one frame later, because the element transitions deliberately snap on a
    /// first appear — that later change is what animates. The exit flips it back and holds the popup open for the
    /// slide's duration.
    /// </summary>
    public partial bool IsSidePanelShown { get; private set; }

    /// <summary>Opens the folders side panel at the leading edge (a no-op unless the side-panel layout is active).</summary>
    public void ShowFoldersPanel() => ShowSidePanel(TimerSidePanelSide.Left);

    /// <summary>Opens the history side panel at the trailing edge (a no-op unless the side-panel layout is active).</summary>
    public void ShowHistoryPanel() => ShowSidePanel(TimerSidePanelSide.Right);

    /// <summary>Resolves the live footprint of a side panel (the card reads its own size and slide distance from it).</summary>
    /// <param name="side">The edge the panel docks to.</param>
    /// <returns>The resolved footprint.</returns>
    internal TimerSidePanelLayout SidePanelLayout(TimerSidePanelSide side) =>
        TimerSidePanelLayoutResolver.Resolve(
            ViewportWidth,
            ViewportHeight,
            _window.TitleBarHeight,
            TimerNormalLayoutResolver.Resolve(ViewportWidth, ViewportHeight).FooterHeight,
            side);

    /// <summary>
    /// Closes an open side panel at once, skipping its exit transition — used when the layout preference switches away
    /// from the side-panel mode or the window enters the compact overlay, where the launching command is about to
    /// disappear and a lingering panel would outlive its own entry point.
    /// </summary>
    public void CloseSidePanelImmediately()
    {
        if (_panelHandle is null)
        {
            return;
        }

        _panelExiting = true;
        _panelHandle.Close();
        ClearSidePanel();
    }

    private void ShowSidePanel(TimerSidePanelSide side)
    {
        if (!_store.Bootstrap.IsSuccess
            || IsCompactPresenter
            || Preferences.LayoutMode != Core.Settings.TimerLayoutMode.SidePanel
            || _panelHandle is not null
            || !CanShowSidePanel())
        {
            return;
        }

        Control panel = side == TimerSidePanelSide.Left
            ? new TimerFoldersPanelControl(Chrome, CloseSidePanel)
            : new TimerHistoryPanelControl(Chrome);

        _panelExiting = false;
        IsSidePanelShown = false;
        _panelHandle = _window.Popups.OpenLive(
            panel,
            new SidePanelAnchorProvider(this, side),
            PopupPlacement.Full,
            isLightDismissEnabled: true,
            gap: 0f,
            chrome: null,
            onClosing: OnSidePanelClosing,
            takeFocus: true,
            automation: new PopupAutomation(SemanticsRole.Pane, SidePanelAutomation(side)));
        _panelHandle.Closed += ClearSidePanel;

        // One dispatcher turn later the card is told it has arrived, and the translate + fade animate to their settled
        // values. The tick also guarantees the window's animator has bound by the time the targets move.
        ScheduleSidePanelStep(FrameDelay, () => IsSidePanelShown = true);
    }

    /// <summary>Requests the panel close through the same exit transition a light dismiss plays.</summary>
    private void CloseSidePanel() => _panelHandle?.Close();

    // The popup foundation's close veto. A chrome-less popup is torn down instantly, so the FIRST close request (a
    // click outside, Esc, or a folder selection) is vetoed and used to start the card's slide-out; the panel then
    // re-requests the close once the transition has played, and this second pass lets it through. Returning true =
    // "cancel this close".
    private bool OnSidePanelClosing()
    {
        if (_panelExiting)
        {
            return false;
        }

        _panelExiting = true;
        IsSidePanelShown = false;
        ScheduleSidePanelStep(TimerSidePanelCard.SlideDuration, () => _panelHandle?.Close());
        return true;
    }

    private void ClearSidePanel()
    {
        _panelTimer?.Stop();
        _panelTimer = null;
        _panelHandle = null;
        _panelExiting = false;
        IsSidePanelShown = false;
    }

    private void ScheduleSidePanelStep(TimeSpan delay, Action step)
    {
        _panelTimer?.Stop();
        DispatcherQueueTimer timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _panelTimer = timer;
        timer.Interval = delay;
        timer.IsRepeating = false;
        timer.Tick += OnTick;
        timer.Start();

        void OnTick(object? sender, EventArgs e)
        {
            timer.Tick -= OnTick;
            timer.Stop();
            if (!ReferenceEquals(_panelTimer, timer))
            {
                return;
            }

            _panelTimer = null;
            step();
        }
    }

    private bool CanShowSidePanel() => TimerSidePanelLayoutResolver.CanShow(
        ViewportWidth,
        ViewportHeight,
        _window.TitleBarHeight,
        TimerNormalLayoutResolver.Resolve(ViewportWidth, ViewportHeight).FooterHeight);

    private static AutomationInfo SidePanelAutomation(TimerSidePanelSide side) =>
        side == TimerSidePanelSide.Left
            ? new AutomationInfo { Name = Resources.Folders, AutomationId = TimerAutomationIds.FoldersPanel }
            : new AutomationInfo { Name = Resources.History, AutomationId = TimerAutomationIds.HistoryPanel };

    /// <summary>
    /// Re-resolves the panel's footprint on every popup layout, so it tracks a window resize, a maximize, and a title
    /// bar band change without reopening. Returning <see langword="null"/> retires the popup — which is exactly what
    /// should happen when the window shrinks past the point where a panel still fits.
    /// </summary>
    private sealed class SidePanelAnchorProvider(MainView owner, TimerSidePanelSide side) : IPopupAnchorProvider
    {
        public PopupAnchorSnapshot? GetSnapshot()
        {
            if (!owner.CanShowSidePanel())
            {
                return null;
            }

            TimerSidePanelLayout layout = owner.SidePanelLayout(side);

            // The available bounds ARE the panel rectangle, and Full fills them — so the resolver alone decides the
            // placement and the solver has nothing left to flip or clamp.
            Rect bounds = Rect.FromXywh(layout.Left, layout.Top, layout.Width, layout.Height);
            return new PopupAnchorSnapshot(bounds, bounds);
        }
    }
}
