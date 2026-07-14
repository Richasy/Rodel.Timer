namespace Rodel.Timer.Core.History;

public sealed record TimerEntry
{
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

    public Guid Id { get; }

    public Guid FolderId { get; }

    public string Title { get; }

    public long DurationSeconds { get; }

    public DateTimeOffset CreatedAt { get; }

    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

    public TimerEntry MoveTo(Guid folderId) => new(Id, folderId, Title, DurationSeconds, CreatedAt);
}
