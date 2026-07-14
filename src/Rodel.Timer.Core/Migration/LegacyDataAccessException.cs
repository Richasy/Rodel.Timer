namespace Rodel.Timer.Core.Migration;

/// <summary>
/// Thrown when legacy Work-Timer data could not be read from its storage (I/O or platform-API failure).
/// This signals an access problem, not that the data itself is malformed; compare
/// <see cref="LegacyDataMigrationException"/>, which signals structurally invalid legacy content.
/// </summary>
public sealed class LegacyDataAccessException : Exception
{
    /// <summary>Creates the exception with a message describing the failed access and its underlying cause.</summary>
    /// <param name="message">A message describing what could not be read.</param>
    /// <param name="innerException">The underlying I/O or platform exception.</param>
    public LegacyDataAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
