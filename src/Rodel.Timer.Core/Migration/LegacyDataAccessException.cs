namespace Rodel.Timer.Core.Migration;

public sealed class LegacyDataAccessException : Exception
{
    public LegacyDataAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
