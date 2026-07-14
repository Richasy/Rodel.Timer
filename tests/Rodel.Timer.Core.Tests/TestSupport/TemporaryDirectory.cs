namespace Rodel.Timer.Core.Tests.TestSupport;

internal sealed class TemporaryDirectory : IDisposable
{
    private bool _disposed;

    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"rodel-timer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Directory.Delete(Path, recursive: true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
