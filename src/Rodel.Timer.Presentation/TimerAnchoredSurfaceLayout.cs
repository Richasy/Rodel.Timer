namespace Rodel.Timer.Presentation;

/// <summary>The deterministic shared geometry of a Folders or History anchored surface.</summary>
/// <param name="Width">The fixed surface width.</param>
/// <param name="Height">The content-derived surface height, capped for scrolling.</param>
public readonly record struct TimerAnchoredSurfaceLayout(float Width, float Height);

/// <summary>Resolves one consistent anchored-surface footprint from its row count and extent.</summary>
public static class TimerAnchoredSurfaceLayoutResolver
{
    /// <summary>The shared anchored-surface width.</summary>
    public const float Width = 336f;

    /// <summary>The maximum surface height before the list scrolls.</summary>
    public const float MaximumHeight = 420f;

    /// <summary>Resolves a surface height while reserving one empty-state row when there are no items.</summary>
    public static TimerAnchoredSurfaceLayout Resolve(int itemCount, float itemExtent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemExtent);
        float height = MathF.Min(
            MaximumHeight,
            108f + (Math.Max(1, itemCount) * (itemExtent + 8f)));
        return new(Width, height);
    }
}
