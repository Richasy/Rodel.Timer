namespace Rodel.Timer.Core.Persistence;

/// <summary>
/// The base type for exceptions raised by <see cref="TimerDataRepository"/>, carrying the path of the
/// timer data file involved. Callers typically catch this base type rather than its specific subclasses
/// when they only need to react to "the repository failed" without distinguishing the cause.
/// </summary>
public class TimerDataException : Exception
{
    /// <summary>Creates the exception with a message, the affected file path, and an optional underlying cause.</summary>
    /// <param name="message">A message describing the failure.</param>
    /// <param name="filePath">The path of the timer data file involved.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public TimerDataException(string message, string filePath, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }

    /// <summary>The path of the timer data file involved in the failure.</summary>
    public string FilePath { get; }
}

/// <summary>
/// Thrown when a repository read or write fails at the I/O level (permissions, sharing violation, missing
/// directory, and similar) rather than because the data itself is invalid; compare
/// <see cref="TimerDataCorruptedException"/>.
/// </summary>
public sealed class TimerDataAccessException : TimerDataException
{
    /// <summary>Creates the exception describing which operation failed on which file.</summary>
    /// <param name="operation">A short description of the failed operation (for example "read" or "write").</param>
    /// <param name="filePath">The path of the timer data file involved.</param>
    /// <param name="innerException">The underlying I/O exception.</param>
    public TimerDataAccessException(string operation, string filePath, Exception innerException)
        : base($"Could not {operation} timer data at '{filePath}'.", filePath, innerException)
    {
    }
}

/// <summary>
/// Thrown when the timer data file was read successfully but its content is invalid (malformed JSON or an
/// unsupported schema version); compare <see cref="TimerDataAccessException"/>, which signals an I/O-level failure.
/// </summary>
public sealed class TimerDataCorruptedException : TimerDataException
{
    /// <summary>Creates the exception for a data file whose content could not be parsed as a valid workspace.</summary>
    /// <param name="filePath">The path of the invalid timer data file.</param>
    /// <param name="innerException">The underlying parsing or validation exception.</param>
    public TimerDataCorruptedException(string filePath, Exception innerException)
        : base($"Timer data at '{filePath}' is invalid.", filePath, innerException)
    {
    }
}

/// <summary>
/// Thrown when a timer data file's schema version is not one this version of the application supports.
/// <see cref="TimerDataRepository"/> wraps this in a <see cref="TimerDataCorruptedException"/> so callers
/// can treat any unreadable data uniformly.
/// </summary>
public sealed class UnsupportedTimerDataVersionException : Exception
{
    /// <summary>Creates the exception describing the mismatch between the actual and supported schema versions.</summary>
    /// <param name="actualVersion">The schema version found in the data file.</param>
    /// <param name="supportedVersion">The schema version this application supports.</param>
    public UnsupportedTimerDataVersionException(int actualVersion, int supportedVersion)
        : base($"Timer data schema {actualVersion} is not supported; expected schema {supportedVersion}.")
    {
        ActualVersion = actualVersion;
        SupportedVersion = supportedVersion;
    }

    /// <summary>The schema version found in the data file.</summary>
    public int ActualVersion { get; }

    /// <summary>The schema version this application supports.</summary>
    public int SupportedVersion { get; }
}
