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
/// Pure, testable policy for the "what's new" notice. A brand-new install (<see cref="TimerBootstrapOrigin.Created"/>)
/// must <b>never</b> show the upgrade notice — its content describes changes since a previous version the user never
/// ran — so a Created workspace only records the current version as seen. An existing or migrated workspace shows the
/// notice only when its persisted last-seen version differs from the current version.
/// </summary>
public static class WhatsNewPolicy
{
    /// <summary>Decides the what's-new action for a bootstrap origin and version pair.</summary>
    /// <param name="origin">Which bootstrap path produced the workspace.</param>
    /// <param name="lastSeenVersion">The persisted last-seen version, or <see langword="null"/> when never recorded.</param>
    /// <param name="currentVersion">The current product version.</param>
    /// <returns>
    /// <see cref="WhatsNewAction.None"/> when the last-seen version already equals the current version;
    /// <see cref="WhatsNewAction.RecordOnly"/> for a fresh <see cref="TimerBootstrapOrigin.Created"/> install with a
    /// differing (typically <see langword="null"/>) last-seen version; otherwise <see cref="WhatsNewAction.Show"/>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="currentVersion"/> is <see langword="null"/> or whitespace.</exception>
    public static WhatsNewAction Decide(TimerBootstrapOrigin origin, string? lastSeenVersion, string currentVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentVersion);

        if (string.Equals(lastSeenVersion, currentVersion, StringComparison.Ordinal))
        {
            return WhatsNewAction.None;
        }

        return origin == TimerBootstrapOrigin.Created ? WhatsNewAction.RecordOnly : WhatsNewAction.Show;
    }
}
