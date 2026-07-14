using System.Text.Json;

using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Persistence;

public sealed class TimerDataRepository : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public TimerDataRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public async Task<TimerWorkspace?> LoadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            try
            {
                await using FileStream stream = new(
                    FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                TimerDataDocument? document = await JsonSerializer.DeserializeAsync(
                        stream,
                        TimerDataJsonContext.Default.TimerDataDocument,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (document is null)
                {
                    throw new JsonException("The timer data document is empty.");
                }

                return TimerDataMapper.FromDocument(document);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnsupportedTimerDataVersionException exception)
            {
                throw new TimerDataCorruptedException(FilePath, exception);
            }
            catch (Exception exception) when (
                exception is JsonException
                    or InvalidDataException
                    or ArgumentException
                    or InvalidOperationException)
            {
                throw new TimerDataCorruptedException(FilePath, exception);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new TimerDataAccessException("read", FilePath, exception);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        TimerWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(workspace);
        TimerDataDocument document = TimerDataMapper.ToDocument(workspace);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        string temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            try
            {
                string? directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using (FileStream stream = new(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(
                            stream,
                            document,
                            TimerDataJsonContext.Default.TimerDataDocument,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporaryPath, FilePath, overwrite: true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException
                    or UnauthorizedAccessException
                    or JsonException)
            {
                throw new TimerDataAccessException("write", FilePath, exception);
            }
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The destination write has already failed or completed; a stale uniquely named temp file is harmless.
        }
    }
}
