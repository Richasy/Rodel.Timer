namespace Rodel.Timer.Core.Folders;

/// <summary>
/// An immutable folder that groups <see cref="History.TimerEntry"/> records under a name and icon.
/// A workspace always has at least one folder (see <see cref="Workspace.TimerWorkspace"/>); this type
/// only enforces its own field invariants, not workspace-wide uniqueness.
/// </summary>
public sealed record TimerFolder
{
    /// <summary>Creates a folder, trimming <paramref name="name"/> to non-whitespace content.</summary>
    /// <param name="id">The folder's identifier. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="name">The display name. Must not be null, empty, or whitespace-only.</param>
    /// <param name="createdAt">The folder's creation timestamp.</param>
    /// <param name="icon">The icon shown for this folder.</param>
    /// <exception cref="ArgumentException"><paramref name="id"/> is <see cref="Guid.Empty"/>, or <paramref name="name"/> is null, empty, or whitespace-only.</exception>
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

    /// <summary>The folder's identifier. Stable for the folder's lifetime; never <see cref="Guid.Empty"/>.</summary>
    public Guid Id { get; }

    /// <summary>The trimmed, non-empty display name.</summary>
    public string Name { get; }

    /// <summary>The timestamp at which the folder was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>The icon shown for this folder.</summary>
    public TimerFolderIcon Icon { get; }

    /// <summary>Returns a copy of this folder with a new <paramref name="name"/> and <paramref name="icon"/>, keeping <see cref="Id"/> and <see cref="CreatedAt"/>.</summary>
    /// <param name="name">The new display name. Must not be null, empty, or whitespace-only.</param>
    /// <param name="icon">The new icon.</param>
    /// <returns>A new <see cref="TimerFolder"/> instance with the updated details.</returns>
    public TimerFolder WithDetails(string name, TimerFolderIcon icon) => new(Id, name, CreatedAt, icon);

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
