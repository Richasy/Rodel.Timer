using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Bootstrap;

/// <summary>
/// Resolves the workspace an application run should use, in priority order: (1) a workspace already
/// persisted by <see cref="TimerDataRepository"/>, (2) a workspace recovered by migrating legacy
/// Work-Timer data, or (3) a brand-new workspace seeded with a single default folder. Cases (2) and
/// (3) are persisted immediately so subsequent runs take path (1).
/// </summary>
public sealed class TimerDataBootstrapper
{
    private readonly TimerDataRepository _repository;
    private readonly ILegacyWorkTimerDataSource _legacyDataSource;
    private readonly LegacyWorkTimerImporter _importer;
    private readonly TimeProvider _timeProvider;
    private readonly string _defaultFolderName;
    private readonly TimerPreferences _defaultPreferences;
    private readonly Func<Guid> _idFactory;

    /// <summary>
    /// Creates a bootstrapper wired to a specific repository, legacy data source, importer, and defaults.
    /// </summary>
    /// <param name="repository">The repository that stores and loads the current-format workspace.</param>
    /// <param name="legacyDataSource">Supplies legacy Work-Timer data when no current workspace exists yet.</param>
    /// <param name="importer">Converts legacy Work-Timer data into a new <see cref="TimerWorkspace"/>.</param>
    /// <param name="timeProvider">The time source used to timestamp any folder or workspace created here.</param>
    /// <param name="defaultFolderName">The name given to the single folder of a freshly created workspace.</param>
    /// <param name="defaultPreferences">The preferences applied to a freshly created workspace.</param>
    /// <param name="idFactory">
    /// Optional identifier factory used for the initial folder and any migration fallback id; defaults to
    /// <see cref="Guid.NewGuid"/>. Overriding this makes bootstrap deterministic for tests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="repository"/>, <paramref name="legacyDataSource"/>, <paramref name="importer"/>,
    /// <paramref name="timeProvider"/>, or <paramref name="defaultPreferences"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="defaultFolderName"/> is null, empty, or whitespace.</exception>
    public TimerDataBootstrapper(
        TimerDataRepository repository,
        ILegacyWorkTimerDataSource legacyDataSource,
        LegacyWorkTimerImporter importer,
        TimeProvider timeProvider,
        string defaultFolderName,
        TimerPreferences defaultPreferences,
        Func<Guid>? idFactory = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(legacyDataSource);
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultFolderName);
        ArgumentNullException.ThrowIfNull(defaultPreferences);

        _repository = repository;
        _legacyDataSource = legacyDataSource;
        _importer = importer;
        _timeProvider = timeProvider;
        _defaultFolderName = defaultFolderName.Trim();
        _defaultPreferences = defaultPreferences;
        _idFactory = idFactory ?? Guid.NewGuid;
    }

    /// <summary>
    /// Loads or produces the workspace to use for this run, persisting it when it was migrated or newly created
    /// so subsequent calls (and future application runs) find it via <see cref="TimerDataRepository.LoadAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the load, migration import, or save.</param>
    /// <returns>The resolved workspace, its <see cref="TimerBootstrapOrigin"/>, and any migration warnings.</returns>
    /// <exception cref="Migration.LegacyDataMigrationException">The legacy data is present but structurally invalid.</exception>
    /// <exception cref="Migration.LegacyDataAccessException">The legacy data source could not be read.</exception>
    /// <exception cref="Persistence.TimerDataException">The repository could not load or save the workspace.</exception>
    public async Task<TimerBootstrapResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        TimerWorkspace? existing = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return new TimerBootstrapResult(
                existing,
                TimerBootstrapOrigin.Existing,
                Array.Empty<string>());
        }

        LegacyWorkTimerSnapshot? legacy = await _legacyDataSource
            .ReadAsync(cancellationToken)
            .ConfigureAwait(false);

        if (legacy is { HasData: true })
        {
            LegacyImportResult imported = _importer.Import(
                legacy,
                NextId(),
                _timeProvider.GetUtcNow());
            await _repository.SaveAsync(imported.Workspace, cancellationToken).ConfigureAwait(false);

            return new TimerBootstrapResult(
                imported.Workspace,
                TimerBootstrapOrigin.Migrated,
                imported.Warnings);
        }

        TimerFolder initialFolder = new(
            NextId(),
            _defaultFolderName,
            _timeProvider.GetUtcNow(),
            TimerFolderIcon.Activity);
        TimerWorkspace created = TimerWorkspace.Create(initialFolder, _defaultPreferences);
        await _repository.SaveAsync(created, cancellationToken).ConfigureAwait(false);

        return new TimerBootstrapResult(
            created,
            TimerBootstrapOrigin.Created,
            Array.Empty<string>());
    }

    private Guid NextId()
    {
        Guid id = _idFactory();
        return id != Guid.Empty
            ? id
            : throw new InvalidOperationException("The id factory returned an empty Guid.");
    }
}
