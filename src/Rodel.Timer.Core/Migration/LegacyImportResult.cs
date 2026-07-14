using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Migration;

public sealed record LegacyImportResult(
    TimerWorkspace Workspace,
    IReadOnlyList<string> Warnings);
