namespace Rodel.Timer.Core.Persistence;

public class TimerDataException : Exception
{
    public TimerDataException(string message, string filePath, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}

public sealed class TimerDataAccessException : TimerDataException
{
    public TimerDataAccessException(string operation, string filePath, Exception innerException)
        : base($"Could not {operation} timer data at '{filePath}'.", filePath, innerException)
    {
    }
}

public sealed class TimerDataCorruptedException : TimerDataException
{
    public TimerDataCorruptedException(string filePath, Exception innerException)
        : base($"Timer data at '{filePath}' is invalid.", filePath, innerException)
    {
    }
}

public sealed class UnsupportedTimerDataVersionException : Exception
{
    public UnsupportedTimerDataVersionException(int actualVersion, int supportedVersion)
        : base($"Timer data schema {actualVersion} is not supported; expected schema {supportedVersion}.")
    {
        ActualVersion = actualVersion;
        SupportedVersion = supportedVersion;
    }

    public int ActualVersion { get; }

    public int SupportedVersion { get; }
}
