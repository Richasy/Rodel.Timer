namespace Rodel.Timer.Core.Migration;

/// <summary>
/// Thrown when legacy Work-Timer data was read successfully but is structurally invalid: it is not
/// valid JSON, contains a null/invalid entry, or violates a <see cref="Workspace.TimerWorkspace"/>
/// invariant once converted. This is a hard migration failure distinct from a recoverable field-level
/// issue (which is instead recorded as a warning in <see cref="LegacyImportResult.Warnings"/>) and
/// distinct from <see cref="LegacyDataAccessException"/>, which signals the data could not be read at all.
/// </summary>
public sealed class LegacyDataMigrationException : Exception
{
    /// <summary>Creates the exception with a message describing the structural failure.</summary>
    /// <param name="message">A message describing what part of the legacy data is invalid.</param>
    public LegacyDataMigrationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the underlying parsing or validation exception.</summary>
    /// <param name="message">A message describing what part of the legacy data is invalid.</param>
    /// <param name="innerException">The underlying JSON, argument, or invalid-operation exception.</param>
    public LegacyDataMigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
