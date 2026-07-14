namespace Rodel.Timer.Core.Migration;

public sealed record LegacyWorkTimerSnapshot(
    string? FolderListJson,
    string? HistoryListJson,
    IReadOnlyDictionary<string, string> Settings)
{
    public bool HasData =>
        !string.IsNullOrWhiteSpace(FolderListJson)
        || !string.IsNullOrWhiteSpace(HistoryListJson)
        || Settings.Count > 0;
}
