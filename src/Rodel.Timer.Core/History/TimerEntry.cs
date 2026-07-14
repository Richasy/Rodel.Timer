namespace Rodel.Timer.Core.History;

/// <summary>
/// An immutable record of one completed timing session: how long it ran, when it was created, and which
/// <see cref="Folders.TimerFolder"/> it belongs to. A workspace's total <see cref="Duration"/> for a day is
/// computed from these entries by <see cref="Workspace.TimerWorkspace.GetTotalDuration"/>.
/// </summary>
public sealed record TimerEntry
{
    /// <summary>Creates a history entry, trimming <paramref name="title"/> to non-whitespace content.</summary>
    /// <param name="id">The entry's identifier. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="folderId">The identifier of the owning folder. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="title">The entry's title. Must not be null, empty, or whitespace-only.</param>
    /// <param name="durationSeconds">The recorded duration, in whole seconds. Must not be negative.</param>
    /// <param name="createdAt">The timestamp at which the timed session finished.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="id"/> or <paramref name="folderId"/> is <see cref="Guid.Empty"/>, or <paramref name="title"/>
    /// is null, empty, or whitespace-only.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="durationSeconds"/> is negative.</exception>
    public TimerEntry(
        Guid id,
        Guid folderId,
        string title,
        long durationSeconds,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A history entry id cannot be empty.", nameof(id));
        }

        if (folderId == Guid.Empty)
        {
            throw new ArgumentException("A folder id cannot be empty.", nameof(folderId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegative(durationSeconds);

        Id = id;
        FolderId = folderId;
        Title = title.Trim();
        DurationSeconds = durationSeconds;
        CreatedAt = createdAt;
    }

    /// <summary>The entry's identifier. Stable for the entry's lifetime; never <see cref="Guid.Empty"/>.</summary>
    public Guid Id { get; }

    /// <summary>The identifier of the folder this entry belongs to.</summary>
    public Guid FolderId { get; }

    /// <summary>The trimmed, non-empty title.</summary>
    public string Title { get; }

    /// <summary>The recorded duration, in whole seconds; never negative.</summary>
    public long DurationSeconds { get; }

    /// <summary>The timestamp at which the timed session finished.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>The recorded duration expressed as a <see cref="TimeSpan"/>, derived from <see cref="DurationSeconds"/>.</summary>
    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

    /// <summary>Returns a copy of this entry re-homed under <paramref name="folderId"/>, keeping every other field.</summary>
    /// <param name="folderId">The identifier of the folder the returned entry should belong to.</param>
    /// <returns>A new <see cref="TimerEntry"/> instance with the updated folder.</returns>
    public TimerEntry MoveTo(Guid folderId) => new(Id, folderId, Title, DurationSeconds, CreatedAt);
}
