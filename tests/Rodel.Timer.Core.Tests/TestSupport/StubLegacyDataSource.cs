using Rodel.Timer.Core.Migration;

namespace Rodel.Timer.Core.Tests.TestSupport;

internal sealed class StubLegacyDataSource(
    LegacyWorkTimerSnapshot? snapshot) : ILegacyWorkTimerDataSource
{
    public int ReadCount { get; private set; }

    public ValueTask<LegacyWorkTimerSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCount++;
        return ValueTask.FromResult(snapshot);
    }
}
