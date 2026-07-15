namespace Rodel.Timer.Presentation;

/// <summary>
/// Stable outbound links shown in the shell. Kept in the presentation layer (rather than as an app-private constant) so
/// a test can pin the exact URL — a wrong link ships a 404 to store users.
/// </summary>
public static class TimerLinks
{
    /// <summary>
    /// The public, archived Work-Timer source repository on GitHub. Rodel Timer is a from-scratch successor whose own
    /// source is not published; the settings "open source" link therefore points at the archived Work-Timer code (the
    /// only public repository), and its label makes that explicit so it does not claim the private source is public.
    /// </summary>
    public const string OpenSourceUri = "https://github.com/Richasy/Work-Timer";
}
