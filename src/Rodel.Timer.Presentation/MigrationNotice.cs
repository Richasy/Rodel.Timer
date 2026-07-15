using Rodel.Timer.Core.Bootstrap;

namespace Rodel.Timer.Presentation;

/// <summary>
/// Pure, testable policy for the startup migration-warning notice. When the workspace was recovered by migrating legacy
/// Work-Timer data (<see cref="TimerBootstrapOrigin.Migrated"/>) and that recovery raised one or more non-fatal
/// warnings, the shell surfaces them once — non-blocking — so the user can review anything the migration repaired.
/// </summary>
public static class MigrationNotice
{
    /// <summary>Decides whether the migration-warning notice should be shown for a bootstrap result.</summary>
    /// <param name="result">The bootstrap result to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="result"/> was produced by migration and carries at least one
    /// warning; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static bool ShouldShow(TimerBootstrapResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Origin == TimerBootstrapOrigin.Migrated && result.Warnings.Count > 0;
    }
}
