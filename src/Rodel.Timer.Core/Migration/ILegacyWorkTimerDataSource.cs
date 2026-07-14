namespace Rodel.Timer.Core.Migration;

/// <summary>
/// Reads legacy Work-Timer data from wherever the previous application version stored it, if any.
/// Implementations return <see langword="null"/> when no legacy data exists (a fresh install, or a
/// non-packaged environment that never had legacy storage); they must not throw for that case.
/// </summary>
public interface ILegacyWorkTimerDataSource
{
    /// <summary>Reads the legacy Work-Timer data, if present.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The legacy snapshot, or <see langword="null"/> if no legacy data exists.</returns>
    /// <exception cref="LegacyDataAccessException">The legacy storage exists but could not be read.</exception>
    ValueTask<LegacyWorkTimerSnapshot?> ReadAsync(CancellationToken cancellationToken = default);
}
