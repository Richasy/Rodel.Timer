namespace Rodel.Timer.Presentation;

/// <summary>
/// Stable outbound links shown in the shell. Kept in the presentation layer (rather than as an app-private constant) so
/// a test can pin the exact URL — a wrong link ships a 404 to store users.
/// </summary>
public static class TimerLinks
{
    /// <summary>
    /// The Rodel Timer source repository on GitHub. The project is open source, so the settings "open source" link
    /// points at the app's own public repository. Kept here (rather than as an app-private constant) so a test can pin
    /// the exact URL — a wrong link ships a 404 to store users.
    /// </summary>
    public const string OpenSourceUri = "https://github.com/Richasy/Rodel.Timer";
}
