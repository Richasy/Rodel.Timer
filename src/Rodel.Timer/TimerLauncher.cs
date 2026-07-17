using System.ComponentModel;
using System.Diagnostics;

namespace Rodel.Timer;

/// <summary>
/// The app's outbound-navigation seam: opens external links in the user's default handler. Sprout's core opens
/// nothing (a <c>HyperlinkButton</c> only carries the URI), so the shell wires the actual launch here.
/// </summary>
internal static class TimerLauncher
{
    /// <summary>
    /// Opens <paramref name="url"/> in the user's default browser via the shell. A missing or blocked handler is
    /// swallowed — a dead link must never crash the app.
    /// </summary>
    /// <param name="url">The absolute URL to open.</param>
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            // No registered handler for the scheme, or the shell refused — ignore.
        }
        catch (InvalidOperationException)
        {
            // Malformed start info — ignore.
        }
    }
}
