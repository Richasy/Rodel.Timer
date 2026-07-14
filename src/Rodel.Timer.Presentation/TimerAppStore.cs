using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Timing;
using Rodel.Timer.Core.Workspace;

using Sprout.Pacing;
using Sprout.Reactive;

namespace Rodel.Timer.Presentation;

/// <summary>
/// The reactive application store over <see cref="TimerWorkspace"/>: it owns the asynchronous bootstrap, the
/// debounced auto-save, the running <see cref="TimerSession"/>, and the UI-thread snapshot caches the view binds
/// to. It is a headless, dependency-injected <c>[Store]</c> — every side effect is a narrow injected delegate
/// (load, save), so the whole surface is asserted with <c>new TimerAppStore(...)</c> and no window.
/// </summary>
/// <remarks>
/// <para>
/// <b>The workspace is never a field.</b> The single aggregate reference lives inside <see cref="Bootstrap"/>'s
/// <see cref="AsyncValue{T}"/> (the latest-wins operation's state): a successful load commits the workspace and
/// makes it visible in the <em>same</em> signal write, so there is no window where the store reports "loaded" but
/// has no workspace, and no second field to keep in sync. A load error or cancel keeps the previous workspace
/// (keep-previous), so the aggregate reference survives a failed refresh.
/// </para>
/// <para>
/// <b>Mutations bump <see cref="Revision"/>.</b> <see cref="TimerWorkspace"/> is mutated in place, so its identity
/// never changes and its <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}"/> views cannot be handed
/// to a reconciling list (they would compare equal to themselves and never refresh). Every mutation therefore
/// increments <see cref="Revision"/> and the store rebuilds fresh <see cref="TimerFolder"/> / <see cref="TimerEntry"/>
/// arrays keyed by <c>(workspace reference, revision)</c> — same revision returns the same snapshot reference; any
/// mutation (or a newly loaded workspace) returns a new one.
/// </para>
/// <para>
/// <b>Threading.</b> The store is UI-thread affine. The injected <see cref="IScheduler"/> marshals every async
/// state commit (bootstrap and save) back to the home thread and drives the debounce timer, so a real app passes a
/// dispatcher scheduler and a test passes a manual one it advances by hand — there is no wall-clock race and no
/// hidden default scheduler.
/// </para>
/// </remarks>
[Store]
public sealed partial class TimerAppStore
{
    private static readonly TimeSpan DefaultSaveDebounceWait = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan DefaultSaveDebounceMaxWait = TimeSpan.FromSeconds(2);

    private readonly Func<TimerWorkspace, CancellationToken, Task> _saveWorkspace;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;
    private readonly Func<Guid> _idFactory;

    private readonly LatestOperation<TimerBootstrapResult> _bootstrap;
    private readonly SequentialOperation<bool> _saveOperation;
    private readonly Debouncer _saveDebouncer;
    private readonly TimerSession _session;

    private Task _lastSaveTask = Task.CompletedTask;
    private TaskCompletionSource? _closeReservation;
    private bool _closing;
    private bool _resourcesDisposed;

    private TimerWorkspace? _snapshotWorkspace;
    private int _snapshotRevision = -1;
    private TimerFolder[] _foldersSnapshot = [];
    private TimerEntry[] _entriesSnapshot = [];
    private TimerEntry[] _selectedEntriesSnapshot = [];
    private TimerFolder? _selectedFolderSnapshot;

    /// <summary>Creates the store over its injected side effects and timing environment.</summary>
    /// <param name="loadWorkspace">
    /// Resolves the workspace for this run (in production, the bootstrapper's load). It is run as a latest-wins
    /// operation whose result becomes <see cref="Bootstrap"/>; a work error is surfaced through
    /// <see cref="Bootstrap"/>'s error phase, not by faulting <see cref="LoadAsync"/>'s task.
    /// </param>
    /// <param name="saveWorkspace">
    /// Persists the current workspace (in production, the repository's save). It is run as a sequential operation
    /// (writes never overlap) whose latest phase is <see cref="SaveState"/>.
    /// </param>
    /// <param name="timeProvider">The time source for folder timestamps, the running session, and "today" totals.</param>
    /// <param name="timeZone">The time zone "today" is measured in for <see cref="TodayTotal"/>.</param>
    /// <param name="scheduler">The home-thread scheduler every async commit is marshaled through and the debounce timer runs on. Required (there is no hidden default), so the store stays explicitly headless.</param>
    /// <param name="idFactory">The identifier factory for created folders and stopped entries; defaults to <see cref="Guid.NewGuid"/>. A returned <see cref="Guid.Empty"/> is rejected.</param>
    /// <param name="saveDebounceWait">The idle period after a change before an auto-save fires; defaults to 500&#160;ms.</param>
    /// <param name="saveDebounceMaxWait">The longest a change may be deferred while edits keep arriving; defaults to 2&#160;s.</param>
    /// <exception cref="ArgumentNullException"><paramref name="loadWorkspace"/>, <paramref name="saveWorkspace"/>, <paramref name="timeProvider"/>, <paramref name="timeZone"/>, or <paramref name="scheduler"/> is <see langword="null"/>.</exception>
    public TimerAppStore(
        Func<CancellationToken, Task<TimerBootstrapResult>> loadWorkspace,
        Func<TimerWorkspace, CancellationToken, Task> saveWorkspace,
        TimeProvider timeProvider,
        TimeZoneInfo timeZone,
        IScheduler scheduler,
        Func<Guid>? idFactory = null,
        TimeSpan? saveDebounceWait = null,
        TimeSpan? saveDebounceMaxWait = null)
    {
        ArgumentNullException.ThrowIfNull(loadWorkspace);
        ArgumentNullException.ThrowIfNull(saveWorkspace);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(scheduler);

        _saveWorkspace = saveWorkspace;
        _timeProvider = timeProvider;
        _timeZone = timeZone;
        _idFactory = idFactory ?? Guid.NewGuid;
        _session = new TimerSession(timeProvider);

        _bootstrap = new LatestOperation<TimerBootstrapResult>(loadWorkspace, scheduler, Lifetime);
        _saveOperation = new SequentialOperation<bool>(SaveWorkAsync, scheduler, Lifetime);
        _saveDebouncer = new Debouncer(
            RequestSave,
            saveDebounceWait ?? DefaultSaveDebounceWait,
            leading: false,
            trailing: true,
            maxWait: saveDebounceMaxWait ?? DefaultSaveDebounceMaxWait,
            scheduler: scheduler);
    }

    /// <summary>
    /// The bootstrap result: <c>Idle</c> until <see cref="LoadAsync"/> runs, then <c>Loading</c> →
    /// <c>Success</c>(<see cref="TimerBootstrapResult"/>) / <c>Error</c>. On success it is the sole reference to the
    /// live <see cref="TimerWorkspace"/>; a failed or cancelled refresh keeps the previous value.
    /// </summary>
    public AsyncValue<TimerBootstrapResult> Bootstrap => _bootstrap.State;

    /// <summary>The latest auto-save phase: <c>Idle</c> before the first save, then <c>Loading</c> → <c>Success</c> / <c>Error</c>. A failed save keeps the state observable here rather than throwing from a mutation.</summary>
    public AsyncValue<bool> SaveState => _saveOperation.State;

    /// <summary>A monotonically increasing counter bumped once per successful workspace mutation. The snapshot getters and every workspace-derived value read it, so a mutation re-runs their subscribers.</summary>
    public partial int Revision { get; private set; }

    /// <summary>Whether a timing session is currently running (set by <see cref="StartTimer"/> / cleared by <see cref="StopTimer"/>).</summary>
    public partial bool IsRunning { get; private set; }

    /// <summary>A reactive tick counter bumped by <see cref="Tick"/> while running, so <see cref="Elapsed"/> re-renders each tick without the store owning a timer.</summary>
    public partial int DisplayTick { get; private set; }

    /// <summary>The folders of the loaded workspace, as a fresh array rebuilt once per <c>(workspace, revision)</c>. Reading it before the workspace has loaded throws.</summary>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded yet.</exception>
    public IReadOnlyList<TimerFolder> Folders
    {
        get
        {
            (TimerWorkspace workspace, int revision) = TrackWorkspace();
            EnsureSnapshot(workspace, revision);
            return _foldersSnapshot;
        }
    }

    /// <summary>Every history entry of the loaded workspace, as a fresh array rebuilt once per <c>(workspace, revision)</c>.</summary>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded yet.</exception>
    public IReadOnlyList<TimerEntry> Entries
    {
        get
        {
            (TimerWorkspace workspace, int revision) = TrackWorkspace();
            EnsureSnapshot(workspace, revision);
            return _entriesSnapshot;
        }
    }

    /// <summary>The history entries belonging to <see cref="SelectedFolder"/>, as a fresh array rebuilt once per <c>(workspace, revision)</c>.</summary>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded yet.</exception>
    public IReadOnlyList<TimerEntry> SelectedEntries
    {
        get
        {
            (TimerWorkspace workspace, int revision) = TrackWorkspace();
            EnsureSnapshot(workspace, revision);
            return _selectedEntriesSnapshot;
        }
    }

    /// <summary>The currently selected folder of the loaded workspace.</summary>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded yet.</exception>
    public TimerFolder SelectedFolder
    {
        get
        {
            (TimerWorkspace workspace, int revision) = TrackWorkspace();
            EnsureSnapshot(workspace, revision);
            return _selectedFolderSnapshot!;
        }
    }

    /// <summary>The loaded workspace's current preferences.</summary>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded yet.</exception>
    public TimerPreferences Preferences
    {
        get
        {
            _ = Revision;
            return Workspace.Preferences;
        }
    }

    /// <summary>
    /// The elapsed time of the running session, or <see cref="TimeSpan.Zero"/> when idle. It reads
    /// <see cref="DisplayTick"/> and <see cref="IsRunning"/> so a tick re-renders it, but the value itself comes from
    /// the session's monotonic clock (immune to wall-clock changes).
    /// </summary>
    public TimeSpan Elapsed
    {
        get
        {
            _ = DisplayTick;
            return IsRunning ? _session.Elapsed : TimeSpan.Zero;
        }
    }

    /// <summary>The wall-clock time the running session started, or <see langword="null"/> when idle. A derived value (not stored state) that reads <see cref="IsRunning"/>.</summary>
    public DateTimeOffset? RunningStartedAt => IsRunning ? _session.StartedAt : null;

    /// <summary>
    /// The total duration of all <em>completed</em> history entries whose creation instant falls on today's local
    /// date (in the store's time zone). It reads <see cref="Revision"/> only — not the tick — so it recomputes when
    /// history changes but not on every tick, and it deliberately excludes the in-flight running session.
    /// </summary>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded yet.</exception>
    public TimeSpan TodayTotal
    {
        get
        {
            _ = Revision;
            TimerWorkspace workspace = Workspace;
            DateTimeOffset localNow = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone);
            DateOnly today = DateOnly.FromDateTime(localNow.DateTime);
            return workspace.GetTotalDuration(today, _timeZone);
        }
    }

    /// <summary>
    /// Resolves the workspace for this run — the one-shot initial load. It runs the latest-wins operation and returns
    /// its task; it never assigns the workspace or bumps <see cref="Revision"/> after the await, because the success
    /// commit and the workspace's visibility are one and the same signal write.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a load-<em>once</em> contract, not a refresh. Once the workspace is available
    /// (<see cref="AsyncValue{T}.HasValue"/> on <see cref="Bootstrap"/> — a success or a kept-previous value) the
    /// aggregate reference is <b>never</b> replaced, so a call after that throws. Reloading a live workspace would
    /// swap the aggregate out from under any pending in-memory edit: an already-armed auto-save would then persist the
    /// freshly loaded workspace over the edit, silently discarding it. The store forbids the reload outright so the
    /// edit and its pending save are safe.
    /// </para>
    /// <para>
    /// Before the first value the load is retryable and latest-wins: a second call while the initial load is still in
    /// flight supersedes (and cancels) the first, and a call after a value-less error retries. Pass
    /// <paramref name="cancellationToken"/> to cancel a specific attempt (its lifetime is also linked to the store's).
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancels this load attempt.</param>
    /// <returns>A task that completes when this load settles (a superseding load supersedes it).</returns>
    /// <exception cref="InvalidOperationException">The workspace has already been loaded (it cannot be reloaded), or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_closing)
        {
            throw new InvalidOperationException("The store is closing and no longer accepts a load.");
        }

        if (Bootstrap.HasValue)
        {
            throw new InvalidOperationException(
                "The workspace has already been loaded; it is a load-once aggregate and cannot be reloaded.");
        }

        return _bootstrap.Run(cancellationToken);
    }

    /// <summary>Selects <paramref name="folderId"/> as the current folder.</summary>
    /// <param name="folderId">The folder to select. Must exist in the workspace.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="folderId"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void SelectFolder(Guid folderId)
    {
        EnsureMutable();
        Workspace.SelectFolder(folderId);
        CommitMutation();
    }

    /// <summary>Adds a new folder with a freshly minted id and the current timestamp.</summary>
    /// <param name="name">The folder name. Must not be null, empty, or whitespace-only.</param>
    /// <param name="icon">The folder icon.</param>
    /// <returns>The newly created folder.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null, empty, or whitespace-only.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, the store is closing, or the id factory returned an empty id.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public TimerFolder AddFolder(string name, TimerFolderIcon icon)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var folder = new TimerFolder(NextId(), name, _timeProvider.GetUtcNow(), icon);
        Workspace.AddFolder(folder);
        CommitMutation();
        return folder;
    }

    /// <summary>Renames and re-icons the folder identified by <paramref name="folderId"/>, keeping its id and creation time.</summary>
    /// <param name="folderId">The folder to update.</param>
    /// <param name="name">The new name. Must not be null, empty, or whitespace-only.</param>
    /// <param name="icon">The new icon.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null, empty, or whitespace-only.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="folderId"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void UpdateFolder(Guid folderId, string name, TimerFolderIcon icon)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        TimerWorkspace workspace = Workspace;
        TimerFolder existing = workspace.Folders.FirstOrDefault(folder => folder.Id == folderId)
            ?? throw new KeyNotFoundException($"Folder '{folderId}' does not exist.");
        workspace.UpdateFolder(existing.WithDetails(name, icon));
        CommitMutation();
    }

    /// <summary>Removes the folder identified by <paramref name="folderId"/>, cascade-deleting its history entries.</summary>
    /// <param name="folderId">The folder to remove.</param>
    /// <returns>The number of history entries removed with the folder.</returns>
    /// <exception cref="InvalidOperationException">The workspace has only one folder, has not been loaded, or the store is closing.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="folderId"/> does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public int RemoveFolder(Guid folderId)
    {
        EnsureMutable();
        int removed = Workspace.RemoveFolder(folderId);
        CommitMutation();
        return removed;
    }

    /// <summary>Moves the history entry identified by <paramref name="entryId"/> to <paramref name="folderId"/>.</summary>
    /// <param name="entryId">The entry to move.</param>
    /// <param name="folderId">The destination folder. Must exist.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="entryId"/> or <paramref name="folderId"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void MoveEntry(Guid entryId, Guid folderId)
    {
        EnsureMutable();
        Workspace.MoveEntry(entryId, folderId);
        CommitMutation();
    }

    /// <summary>Removes the history entry identified by <paramref name="entryId"/>.</summary>
    /// <param name="entryId">The entry to remove.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="entryId"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void RemoveEntry(Guid entryId)
    {
        EnsureMutable();
        Workspace.RemoveEntry(entryId);
        CommitMutation();
    }

    /// <summary>Sets the preferred theme, preserving every other preference.</summary>
    /// <param name="theme">The new theme.</param>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void UpdateTheme(TimerThemeMode theme)
    {
        EnsureMutable();
        TimerPreferences current = Workspace.Preferences;
        Workspace.UpdatePreferences(new TimerPreferences(
            theme,
            current.Language,
            current.FontFamily,
            current.DailyTargetMinutes,
            current.LastSeenVersion));
        CommitMutation();
    }

    /// <summary>Sets the preferred language, preserving every other preference.</summary>
    /// <param name="language">The new language tag. Must be a supported language.</param>
    /// <exception cref="ArgumentException"><paramref name="language"/> is not a supported language.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void UpdateLanguage(string language)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        TimerPreferences current = Workspace.Preferences;
        Workspace.UpdatePreferences(new TimerPreferences(
            current.Theme,
            language,
            current.FontFamily,
            current.DailyTargetMinutes,
            current.LastSeenVersion));
        CommitMutation();
    }

    /// <summary>Sets the preferred font family, preserving every other preference.</summary>
    /// <param name="fontFamily">The new font family. Must not be null, empty, or whitespace-only.</param>
    /// <exception cref="ArgumentException"><paramref name="fontFamily"/> is null, empty, or whitespace-only.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void UpdateFontFamily(string fontFamily)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);
        TimerPreferences current = Workspace.Preferences;
        Workspace.UpdatePreferences(new TimerPreferences(
            current.Theme,
            current.Language,
            fontFamily,
            current.DailyTargetMinutes,
            current.LastSeenVersion));
        CommitMutation();
    }

    /// <summary>Sets the daily time target (in minutes), preserving every other preference.</summary>
    /// <param name="dailyTargetMinutes">The new daily target, in minutes; must be within the allowed range.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dailyTargetMinutes"/> is outside the valid range.</exception>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void UpdateDailyTarget(int dailyTargetMinutes)
    {
        EnsureMutable();
        TimerPreferences current = Workspace.Preferences;
        Workspace.UpdatePreferences(new TimerPreferences(
            current.Theme,
            current.Language,
            current.FontFamily,
            dailyTargetMinutes,
            current.LastSeenVersion));
        CommitMutation();
    }

    /// <summary>Records the application version that last wrote the preferences, preserving every other preference.</summary>
    /// <param name="lastSeenVersion">The version string, or <see langword="null"/>/whitespace to clear it.</param>
    /// <exception cref="InvalidOperationException">The workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void UpdateLastSeenVersion(string? lastSeenVersion)
    {
        EnsureMutable();
        TimerPreferences current = Workspace.Preferences;
        Workspace.UpdatePreferences(new TimerPreferences(
            current.Theme,
            current.Language,
            current.FontFamily,
            current.DailyTargetMinutes,
            lastSeenVersion));
        CommitMutation();
    }

    /// <summary>
    /// Starts a timing session. It captures no folder — the destination is resolved from the selection at the moment
    /// <see cref="StopTimer"/> runs — so switching or deleting the folder while timing re-homes the recorded entry to
    /// whatever folder is current then.
    /// </summary>
    /// <exception cref="InvalidOperationException">A session is already running, the workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void StartTimer()
    {
        EnsureMutable();
        _session.Start();
        IsRunning = true;
    }

    /// <summary>
    /// Stops the running session and records a history entry under the folder selected <em>at this moment</em>, using
    /// the session's monotonic elapsed time. <see cref="IsRunning"/> is cleared before the entry is added.
    /// </summary>
    /// <remarks>
    /// The recording identifier is minted and checked for collision against the current history <b>before</b> the
    /// session is stopped. If the id factory hands back an identifier that already exists, the store throws and leaves
    /// the session running (its <see cref="Elapsed"/> intact) so the stop can be retried once a usable id is
    /// available — the timed duration is never lost. Only after the id is known good does it stop the session and add
    /// the entry; because the destination is the workspace's selected folder (an existing-folder invariant) and the
    /// store is single-threaded, that final add cannot fail.
    /// </remarks>
    /// <param name="title">The entry title (UI-owned; never stored as reactive state). Must not be null, empty, or whitespace-only.</param>
    /// <returns>The recorded history entry.</returns>
    /// <exception cref="ArgumentException"><paramref name="title"/> is null, empty, or whitespace-only (the session keeps running).</exception>
    /// <exception cref="InvalidOperationException">No session is running, the id factory returned an empty or already-used identifier (the session keeps running), the workspace has not been loaded, or the store is closing.</exception>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public TimerEntry StopTimer(string title)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        TimerWorkspace workspace = Workspace;
        Guid entryId = NextId();
        if (workspace.Entries.Any(entry => entry.Id == entryId))
        {
            throw new InvalidOperationException(
                $"The id factory produced identifier '{entryId}', which already belongs to a history entry; the session is left running so the stop can be retried.");
        }

        Guid folderId = workspace.SelectedFolderId;
        TimerEntry entry = _session.Stop(entryId, folderId, title);
        IsRunning = false;
        workspace.AddEntry(entry);
        CommitMutation();
        return entry;
    }

    /// <summary>
    /// Advances the reactive tick so <see cref="Elapsed"/> re-renders. It is a no-op when no session is running. The
    /// store owns no timer — the application drives this from its own UI-thread timer.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public void Tick()
    {
        ThrowIfDisposed();
        if (!IsRunning)
        {
            return;
        }

        DisplayTick++;
    }

    /// <summary>
    /// Flushes any pending debounced save immediately and awaits it. It neither closes the store nor blocks future
    /// mutations, and it does not rethrow a save failure (the outcome stays observable in <see cref="SaveState"/>);
    /// use <see cref="CloseAsync"/> for the shutdown flush that rethrows.
    /// </summary>
    /// <returns>A task that completes when the flushed save settles.</returns>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public Task FlushSaveAsync()
    {
        ThrowIfDisposed();
        _saveDebouncer.Flush();
        return _lastSaveTask;
    }

    /// <summary>
    /// Persists any outstanding change and marks the store closed. Call it on the home thread before
    /// <see cref="System.IDisposable.Dispose"/>. It immediately rejects ordinary mutations, flushes a pending
    /// debounced save (or forces one if the last save had failed), awaits the final save, and rethrows a save failure
    /// with its stack intact.
    /// </summary>
    /// <remarks>
    /// On failure it re-opens the store (mutations resume) and a subsequent <see cref="CloseAsync"/> performs a fresh
    /// save attempt — there is no separate retry method. On success it stays closed until disposal. Concurrent calls
    /// are de-duplicated: they observe the same task.
    /// </remarks>
    /// <returns>A task that completes when the shutdown save settles; it faults with the save error if it failed.</returns>
    /// <exception cref="ObjectDisposedException">The store has been disposed.</exception>
    public Task CloseAsync()
    {
        ThrowIfDisposed();
        if (_closeReservation is { } existing)
        {
            return existing.Task;
        }

        var reservation = new TaskCompletionSource();
        _closeReservation = reservation;
        _ = CloseCoreAsync(reservation);
        return reservation.Task;
    }

    /// <summary>The single aggregate reference, read from the bootstrap signal. Throws until the workspace has loaded.</summary>
    private TimerWorkspace Workspace => Bootstrap.HasValue
        ? Bootstrap.Value!.Workspace
        : throw new InvalidOperationException("The workspace has not been loaded yet.");

    private async Task CloseCoreAsync(TaskCompletionSource reservation)
    {
        try
        {
            _closing = true;

            bool hadPending = _saveDebouncer.IsPending;
            _saveDebouncer.Flush();
            if (!hadPending && SaveState.IsError)
            {
                RequestSave();
            }

            Task pending = _lastSaveTask;
            await pending.ConfigureAwait(false);
            SaveState.Rethrow();
            reservation.TrySetResult();
        }
        catch (Exception exception)
        {
            _closing = false;
            _closeReservation = null;
            reservation.TrySetException(exception);
        }
    }

    private async Task<bool> SaveWorkAsync(CancellationToken cancellationToken)
    {
        TimerWorkspace workspace = Workspace;
        await _saveWorkspace(workspace, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private void RequestSave() => _lastSaveTask = _saveOperation.Run();

    private void CommitMutation()
    {
        Revision++;
        _saveDebouncer.Invoke();
    }

    private (TimerWorkspace Workspace, int Revision) TrackWorkspace()
    {
        int revision = Revision;
        TimerWorkspace workspace = Workspace;
        return (workspace, revision);
    }

    private void EnsureSnapshot(TimerWorkspace workspace, int revision)
    {
        if (ReferenceEquals(_snapshotWorkspace, workspace) && _snapshotRevision == revision)
        {
            return;
        }

        _snapshotWorkspace = workspace;
        _snapshotRevision = revision;
        _foldersSnapshot = [.. workspace.Folders];
        _entriesSnapshot = [.. workspace.Entries];

        Guid selectedId = workspace.SelectedFolderId;
        _selectedFolderSnapshot = Array.Find(_foldersSnapshot, folder => folder.Id == selectedId);
        _selectedEntriesSnapshot = Array.FindAll(_entriesSnapshot, entry => entry.FolderId == selectedId);
    }

    private Guid NextId()
    {
        Guid id = _idFactory();
        return id != Guid.Empty
            ? id
            : throw new InvalidOperationException("The id factory returned an empty Guid.");
    }

    private void EnsureMutable()
    {
        ThrowIfDisposed();
        if (_closing)
        {
            throw new InvalidOperationException("The store is closing and no longer accepts mutations.");
        }

        if (!Bootstrap.HasValue)
        {
            throw new InvalidOperationException("The workspace has not been loaded yet.");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_resourcesDisposed, this);

    /// <summary>Releases the store-owned debounce timer after the generated <c>Dispose</c> has cancelled the lifetime token.</summary>
    partial void OnDispose()
    {
        _resourcesDisposed = true;
        _saveDebouncer.Dispose();
    }
}
