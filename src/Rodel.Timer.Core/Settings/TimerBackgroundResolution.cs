namespace Rodel.Timer.Core.Settings;

/// <summary>
/// The deterministic effective background state after the platform reports whether a requested material is active.
/// </summary>
/// <param name="Requested">The persisted user preference.</param>
/// <param name="IsFallback">Whether a requested material is currently showing its opaque policy fallback.</param>
public readonly record struct TimerBackgroundResolution(
    TimerBackgroundMode Requested,
    bool IsFallback)
{
    /// <summary>Resolves a persisted choice against the platform's post-apply material state.</summary>
    /// <param name="requested">The persisted background preference.</param>
    /// <param name="isMaterialActive">Whether the platform successfully activated the requested material.</param>
    /// <returns>The effective background and fallback state.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="requested"/> is not a defined mode.</exception>
    public static TimerBackgroundResolution Resolve(
        TimerBackgroundMode requested,
        bool isMaterialActive)
    {
        if (!Enum.IsDefined(requested))
        {
            throw new ArgumentOutOfRangeException(nameof(requested));
        }

        return new(requested, IsFallback: !isMaterialActive);
    }
}
