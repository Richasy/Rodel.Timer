namespace Rodel.Timer.Core.Migration;

internal sealed class LegacyFolderData
{
    public string? Name { get; set; }

    public DateTime CreateTime { get; set; }

    public string? Id { get; set; }

    public int Icon { get; set; }
}

internal sealed class LegacyHistoryData
{
    public string? Name { get; set; }

    public string? FolderId { get; set; }

    public string? Id { get; set; }

    public int TotalSeconds { get; set; }

    public DateTime CreateTime { get; set; }
}
