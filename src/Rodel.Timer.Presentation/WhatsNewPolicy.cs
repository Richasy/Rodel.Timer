using Rodel.Timer.Core.Bootstrap;

namespace Rodel.Timer.Presentation;

/// <summary>
/// The action the shell should take for the "what's new" notice after the workspace loads, decided purely from the
/// bootstrap origin and the persisted / current version by <see cref="WhatsNewPolicy"/>.
/// </summary>
public enum WhatsNewAction
{
    /// <summary>Do nothing — the last-seen version already matches the current version.</summary>
    None,

    /// <summary>Silently record the current version as seen, without showing the dialog (a fresh install).</summary>
    RecordOnly,

    /// <summary>Show the what's-new dialog, then record the current version as seen (an upgrade or migration).</summary>
    Show,
}

/// <summary>
/// Pure, testable policy for the "what's new" notice, which answers two independent questions with two different
/// versions.
/// </summary>
/// <remarks>
/// <para>
/// <b>Has this build been greeted yet?</b> — the <em>current</em> version. It is recorded the first time a build runs
/// and compared on every later launch, so the notice can never appear twice for the same build no matter how often the
/// app is restarted.
/// </para>
/// <para>
/// <b>Is there anything to say?</b> — the <em>notice</em> version: the release whose notes the shipped what's-new text
/// actually describes. Because the two are separate, a later release that only fixes bugs simply leaves the notice
/// version behind and upgrades <em>silently</em> (it records the new current version and shows nothing), instead of
/// replaying an older release's notes at a user who has already read them. Writing new notes means bumping the notice
/// version alongside the text; that, and only that, makes the notice appear again.
/// </para>
/// <para>
/// A brand-new install (<see cref="TimerBootstrapOrigin.Created"/>) never shows the notice at all — its content
/// describes changes since a previous version the user never ran.
/// </para>
/// </remarks>
public static class WhatsNewPolicy
{
    /// <summary>Decides the what's-new action for a bootstrap origin and the version triple.</summary>
    /// <param name="origin">Which bootstrap path produced the workspace.</param>
    /// <param name="lastSeenVersion">The persisted last-seen version, or <see langword="null"/> when never recorded.</param>
    /// <param name="currentVersion">The version of the running build; the value recorded once the notice is settled.</param>
    /// <param name="noticeVersion">The release the shipped what's-new text describes. A user who already saw that release's notes is not shown them again.</param>
    /// <returns>
    /// <see cref="WhatsNewAction.None"/> when the last-seen version already equals the current version;
    /// <see cref="WhatsNewAction.RecordOnly"/> for a fresh <see cref="TimerBootstrapOrigin.Created"/> install, and for
    /// an upgrade that carries no unseen notes; otherwise <see cref="WhatsNewAction.Show"/>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="currentVersion"/> or <paramref name="noticeVersion"/> is <see langword="null"/> or whitespace.</exception>
    public static WhatsNewAction Decide(
        TimerBootstrapOrigin origin,
        string? lastSeenVersion,
        string currentVersion,
        string noticeVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(noticeVersion);

        if (string.Equals(lastSeenVersion, currentVersion, StringComparison.Ordinal))
        {
            return WhatsNewAction.None;
        }

        if (origin == TimerBootstrapOrigin.Created)
        {
            return WhatsNewAction.RecordOnly;
        }

        return HasUnseenNotes(lastSeenVersion, noticeVersion)
            ? WhatsNewAction.Show
            : WhatsNewAction.RecordOnly;
    }

    /// <summary>
    /// Whether the shipped notes are newer than the release the user last ran. A never-recorded (<see langword="null"/>)
    /// or unparsable last-seen version counts as unseen — an unknown history is treated as "older", so the notes are
    /// shown rather than silently swallowed.
    /// </summary>
    private static bool HasUnseenNotes(string? lastSeenVersion, string noticeVersion)
    {
        if (!Version.TryParse(noticeVersion, out Version? notice))
        {
            return true;
        }

        return !Version.TryParse(lastSeenVersion, out Version? seen) || seen < notice;
    }
}
