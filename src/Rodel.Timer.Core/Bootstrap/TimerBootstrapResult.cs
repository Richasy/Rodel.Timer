using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Bootstrap;

public enum TimerBootstrapOrigin
{
    Existing,
    Migrated,
    Created,
}

public sealed record TimerBootstrapResult(
    TimerWorkspace Workspace,
    TimerBootstrapOrigin Origin,
    IReadOnlyList<string> Warnings);
