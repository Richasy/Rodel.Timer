using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Bootstrap;

/// <summary>
/// Identifies which of the three bootstrap paths produced a <see cref="TimerWorkspace"/>:
/// an already-persisted workspace, a workspace recovered from legacy Work-Timer data, or a
/// brand-new workspace created because neither of the above was available.
/// </summary>
public enum TimerBootstrapOrigin
{
    /// <summary>A workspace was loaded from the current repository; no migration or creation occurred.</summary>
    Existing,

    /// <summary>No current workspace existed, so one was recovered by migrating legacy Work-Timer data and persisted.</summary>
    Migrated,

    /// <summary>Neither a current workspace nor legacy data existed, so a fresh workspace was created and persisted.</summary>
    Created,
}

/// <summary>
/// The outcome of <see cref="TimerDataBootstrapper.LoadAsync"/>: the resulting workspace, which path produced it,
/// and any non-fatal warnings raised while recovering it (for example during legacy migration).
/// </summary>
/// <param name="Workspace">The workspace to use for this application run.</param>
/// <param name="Origin">Which bootstrap path produced <paramref name="Workspace"/>.</param>
/// <param name="Warnings">
/// Non-fatal issues encountered while producing <paramref name="Workspace"/>. Empty unless
/// <paramref name="Origin"/> is <see cref="TimerBootstrapOrigin.Migrated"/> and the legacy data needed repair.
/// </param>
public sealed record TimerBootstrapResult(
    TimerWorkspace Workspace,
    TimerBootstrapOrigin Origin,
    IReadOnlyList<string> Warnings);
