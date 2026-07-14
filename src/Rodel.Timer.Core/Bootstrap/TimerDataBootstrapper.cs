using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;

namespace Rodel.Timer.Core.Bootstrap;

public sealed class TimerDataBootstrapper
{
    private readonly TimerDataRepository _repository;
    private readonly ILegacyWorkTimerDataSource _legacyDataSource;
    private readonly LegacyWorkTimerImporter _importer;
    private readonly TimeProvider _timeProvider;
    private readonly string _defaultFolderName;
    private readonly TimerPreferences _defaultPreferences;
    private readonly Func<Guid> _idFactory;

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
