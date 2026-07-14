namespace Rodel.Timer.Core.Migration;

public interface ILegacyWorkTimerDataSource
{
    ValueTask<LegacyWorkTimerSnapshot?> ReadAsync(CancellationToken cancellationToken = default);
}
