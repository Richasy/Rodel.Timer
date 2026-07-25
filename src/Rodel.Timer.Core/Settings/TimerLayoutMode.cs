namespace Rodel.Timer.Core.Settings;

/// <summary>
/// How the folders and history surfaces are reached from the shell. The value is a pure presentation preference — it
/// changes only where the two lists are hosted, never the data behind them.
/// </summary>
public enum TimerLayoutMode
{
    /// <summary>
    /// The default: two title-bar drop-down commands open the folders and history lists as anchored flyouts.
    /// </summary>
    Flyout,

    /// <summary>
    /// The footer carries a folders command at its leading edge and a history command at its trailing edge; each opens
    /// a light-dismiss side panel that slides in from that edge, between the title bar and the footer.
    /// </summary>
    SidePanel,
}
