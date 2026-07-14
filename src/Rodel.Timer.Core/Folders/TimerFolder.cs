namespace Rodel.Timer.Core.Folders;

public sealed record TimerFolder
{
    public TimerFolder(Guid id, string name, DateTimeOffset createdAt, TimerFolderIcon icon)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A folder id cannot be empty.", nameof(id));
        }

        Id = id;
        Name = NormalizeName(name);
        CreatedAt = createdAt;
        Icon = icon;
    }

    public Guid Id { get; }

    public string Name { get; }

    public DateTimeOffset CreatedAt { get; }

    public TimerFolderIcon Icon { get; }

    public TimerFolder WithDetails(string name, TimerFolderIcon icon) => new(Id, name, CreatedAt, icon);

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
