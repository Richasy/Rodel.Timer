using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Migration;

/// <summary>
/// The outcome of <see cref="LegacyWorkTimerImporter.Import"/>: the migrated workspace and any non-fatal
/// warnings recorded while repairing recoverable legacy data issues (missing names, unknown icons,
/// out-of-range preferences, orphaned entries, and similar).
/// </summary>
/// <param name="Workspace">The workspace produced from the legacy data.</param>
/// <param name="Warnings">Human-readable descriptions of recoverable issues that were repaired during import; empty if none.</param>
public sealed record LegacyImportResult(
    TimerWorkspace Workspace,
    IReadOnlyList<string> Warnings);
