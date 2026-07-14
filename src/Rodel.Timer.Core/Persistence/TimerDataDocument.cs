using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.Settings;

namespace Rodel.Timer.Core.Persistence;

internal sealed class TimerDataDocument
{
    public int SchemaVersion { get; set; }

    public Guid SelectedFolderId { get; set; }

    public TimerPreferencesData Preferences { get; set; } = new();

    public List<TimerFolderData> Folders { get; set; } = [];

    public List<TimerEntryData> Entries { get; set; } = [];
}

internal sealed class TimerFolderData
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public TimerFolderIcon Icon { get; set; }
}

internal sealed class TimerEntryData
{
    public Guid Id { get; set; }

    public Guid FolderId { get; set; }

    public string Title { get; set; } = string.Empty;

    public long DurationSeconds { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class TimerPreferencesData
{
    public TimerThemeMode Theme { get; set; }

    public string Language { get; set; } = string.Empty;

    public string FontFamily { get; set; } = string.Empty;

    public int DailyTargetMinutes { get; set; }

    public string? LastSeenVersion { get; set; }
}
