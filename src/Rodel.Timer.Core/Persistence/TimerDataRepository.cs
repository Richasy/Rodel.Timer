using System.Text.Json;

using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Persistence;

/// <summary>
/// Loads and saves a single <see cref="TimerWorkspace"/> as a JSON file. A missing file is not an error:
/// <see cref="LoadAsync"/> returns <see langword="null"/> to mean "no workspace has been saved yet", which
/// bootstrap uses to distinguish a first run from a corrupted one. Saves are atomic: <see cref="SaveAsync"/>
/// writes to a uniquely named temporary file and then renames it over the destination, so a crash or
/// power loss during a write can never leave a partially written file at <see cref="FilePath"/>. Concurrent
/// calls on the same instance are serialized by an internal gate, so loads and saves never interleave.
/// </summary>
public sealed class TimerDataRepository : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    /// <summary>Creates a repository backed by the JSON file at <paramref name="filePath"/>.</summary>
    /// <param name="filePath">The path of the timer data file. Resolved to a full path immediately.</param>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is null, empty, or whitespace-only.</exception>
    public TimerDataRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    /// <summary>The full path of the timer data file.</summary>
    public string FilePath { get; }

    /// <summary>
    /// Loads the workspace from <see cref="FilePath"/>, or returns <see langword="null"/> if the file does
    /// not exist (there is no persisted workspace yet).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The persisted workspace, or <see langword="null"/> if none exists.</returns>
    /// <exception cref="TimerDataCorruptedException">The file exists but its content is not a valid workspace.</exception>
    /// <exception cref="TimerDataAccessException">The file exists but could not be read due to an I/O or permission failure.</exception>
    /// <exception cref="ObjectDisposedException">The repository has been disposed.</exception>
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

    /// <summary>
    /// Saves <paramref name="workspace"/> to <see cref="FilePath"/> atomically: the new content is written
    /// to a temporary file and then moved over the destination, so a failure or cancellation partway through
    /// never corrupts or truncates any previously saved data.
    /// </summary>
    /// <param name="workspace">The workspace to persist.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="workspace"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimerDataAccessException">The data could not be written due to an I/O or permission failure.</exception>
    /// <exception cref="ObjectDisposedException">The repository has been disposed.</exception>
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

    /// <summary>Releases the internal synchronization gate. Safe to call more than once.</summary>
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
