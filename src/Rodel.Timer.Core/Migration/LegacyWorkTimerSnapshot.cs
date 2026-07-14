namespace Rodel.Timer.Core.Migration;

/// <summary>
/// The raw legacy Work-Timer data as read from its original storage, before conversion by
/// <see cref="LegacyWorkTimerImporter"/>. The folder and history lists are opaque JSON because the legacy
/// application's schema is not this application's schema; <see cref="LegacyWorkTimerImporter"/> deserializes
/// and validates them.
/// </summary>
/// <param name="FolderListJson">The legacy folder list as JSON, or <see langword="null"/>/empty if none was stored.</param>
/// <param name="HistoryListJson">The legacy history entry list as JSON, or <see langword="null"/>/empty if none was stored.</param>
/// <param name="Settings">The legacy application settings, keyed by their original setting name.</param>
public sealed record LegacyWorkTimerSnapshot(
    string? FolderListJson,
    string? HistoryListJson,
    IReadOnlyDictionary<string, string> Settings)
{
    /// <summary>
    /// Whether this snapshot carries any legacy data at all. <see langword="false"/> means the legacy
    /// application was never used (or left no trace), so bootstrap should create a fresh workspace instead
    /// of attempting a migration.
    /// </summary>
    public bool HasData =>
        !string.IsNullOrWhiteSpace(FolderListJson)
        || !string.IsNullOrWhiteSpace(HistoryListJson)
        || Settings.Count > 0;
}
