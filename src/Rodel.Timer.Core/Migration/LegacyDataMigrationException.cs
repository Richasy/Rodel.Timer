namespace Rodel.Timer.Core.Migration;

public sealed class LegacyDataMigrationException : Exception
{
    public LegacyDataMigrationException(string message)
        : base(message)
    {
    }

    public LegacyDataMigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
