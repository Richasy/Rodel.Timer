using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.TestSupport;

/// <summary>
/// Wires a <see cref="TimerAppStore"/> to fully controllable load / save delegates, a <see cref="QueueingScheduler"/>,
/// and a <see cref="ManualTimeProvider"/> so every store test is deterministic and wall-clock-free: the test decides
/// when a load resolves, when a save runs, whether a save faults or blocks, and what identifiers are minted.
/// </summary>
internal sealed class TimerStoreHarness : IDisposable
{
    /// <summary>A stable identifier for the first seed folder ("Work").</summary>
    public static readonly Guid FolderAId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>A stable identifier for the second seed folder ("Study").</summary>
    public static readonly Guid FolderBId = new("22222222-2222-2222-2222-222222222222");

    private readonly DateTimeOffset _origin = new(2024, 1, 1, 8, 0, 0, TimeSpan.Zero);
    private int _idCounter;
    private TaskCompletionSource<TimerBootstrapResult>? _pendingLoad;

    /// <summary>Creates the harness and its store; the store is <b>not</b> loaded yet.</summary>
    public TimerStoreHarness()
    {
        Time = new ManualTimeProvider(_origin, TimeZoneInfo.Utc);
        Scheduler = new QueueingScheduler { Now = _origin };
        IdFactory = NextDeterministicId;
        Store = new TimerAppStore(
            LoadAsync,
            SaveAsync,
            Time,
            TimeZoneInfo.Utc,
            Scheduler,
            () => IdFactory(),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(2));
    }

    /// <summary>The manual time source.</summary>
    public ManualTimeProvider Time { get; }

    /// <summary>The queueing scheduler the store commits and debounces through.</summary>
    public QueueingScheduler Scheduler { get; }

    /// <summary>The store under test.</summary>
    public TimerAppStore Store { get; }

    /// <summary>The identifier factory the store uses (swappable, e.g. to inject <see cref="Guid.Empty"/>).</summary>
    public Func<Guid> IdFactory { get; set; }

    /// <summary>How many times the load delegate has been invoked.</summary>
    public int LoadCount { get; private set; }

    /// <summary>How many times the save delegate has begun executing.</summary>
    public int SaveCount { get; private set; }

    /// <summary>The workspace reference captured by each save invocation, in order.</summary>
    public List<TimerWorkspace> SavedWorkspaces { get; } = [];

    /// <summary>When set, the next save invocation awaits this gate before completing (to model an in-flight save).</summary>
    public TaskCompletionSource? SaveGate { get; set; }

    /// <summary>When set, the next save invocation throws this exception (consumed once).</summary>
    public Exception? NextSaveError { get; set; }

    /// <summary>Runs <see cref="TimerAppStore.LoadAsync"/> and returns its task without settling it.</summary>
    public Task BeginLoad() => Store.LoadAsync();

    /// <summary>Completes the pending load with <paramref name="result"/> (or a default two-folder workspace).</summary>
    public void CompleteLoad(TimerBootstrapResult? result = null)
    {
        TaskCompletionSource<TimerBootstrapResult> pending = _pendingLoad
            ?? throw new InvalidOperationException("No load is pending.");
        _pendingLoad = null;
        pending.SetResult(result ?? new TimerBootstrapResult(BuildWorkspace(), TimerBootstrapOrigin.Existing, []));
    }

    /// <summary>Faults the pending load with <paramref name="error"/>.</summary>
    public void FailLoad(Exception error)
    {
        TaskCompletionSource<TimerBootstrapResult> pending = _pendingLoad
            ?? throw new InvalidOperationException("No load is pending.");
        _pendingLoad = null;
        pending.SetException(error);
    }

    /// <summary>Loads the store to a ready state: begins a load, completes it with the default workspace, and drains the scheduler.</summary>
    public void LoadToReady(TimerBootstrapResult? result = null)
    {
        BeginLoad();
        CompleteLoad(result);
        Scheduler.DrainUntilIdle();
    }

    /// <summary>Builds a fresh two-folder workspace (A "Work" selected, B "Study") with no history entries.</summary>
    public TimerWorkspace BuildWorkspace()
    {
        var folderA = new TimerFolder(FolderAId, "Work", _origin, TimerFolderIcon.Briefcase);
        var folderB = new TimerFolder(FolderBId, "Study", _origin, TimerFolderIcon.Book);
        return TimerWorkspace.Restore([folderA, folderB], [], FolderAId, TimerPreferences.Default);
    }

    /// <summary>Builds a workspace seeded with the supplied history entries.</summary>
    public TimerWorkspace BuildWorkspace(IEnumerable<TimerEntry> entries)
    {
        var folderA = new TimerFolder(FolderAId, "Work", _origin, TimerFolderIcon.Briefcase);
        var folderB = new TimerFolder(FolderBId, "Study", _origin, TimerFolderIcon.Book);
        return TimerWorkspace.Restore([folderA, folderB], entries, FolderAId, TimerPreferences.Default);
    }

    /// <summary>Mints a deterministic, never-empty identifier.</summary>
    public Guid NextDeterministicId()
    {
        _idCounter++;
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, _idCounter);
        bytes[15] = 0xAA;
        return new Guid(bytes);
    }

    /// <inheritdoc/>
    public void Dispose() => Store.Dispose();

    private Task<TimerBootstrapResult> LoadAsync(CancellationToken cancellationToken)
    {
        LoadCount++;
        var tcs = new TaskCompletionSource<TimerBootstrapResult>();
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        _pendingLoad = tcs;
        return tcs.Task;
    }

    private async Task SaveAsync(TimerWorkspace workspace, CancellationToken cancellationToken)
    {
        SaveCount++;
        SavedWorkspaces.Add(workspace);

        if (SaveGate is { } gate)
        {
            await gate.Task.ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (NextSaveError is { } error)
        {
            NextSaveError = null;
            throw error;
        }
    }
}
