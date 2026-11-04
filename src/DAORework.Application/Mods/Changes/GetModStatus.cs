using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Documents;

namespace DAORework.Application.Mods.Changes;

/// <summary>Reads a documents folder's deployment record, open journal and backups (ROADMAP P2-06).</summary>
public sealed class GetModStatus
{
    private readonly IDeploymentRecordStore _records;
    private readonly IApplyJournalStore _journals;
    private readonly IDeploymentBackupStore _backups;
    private readonly IFileSystem _fileSystem;

    public GetModStatus(IDeploymentRecordStore records, IApplyJournalStore journals, IDeploymentBackupStore backups, IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _records = records;
        _journals = journals;
        _backups = backups;
    }

    public async Task<Result<ModStatus>> ExecuteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        if (!(await _records.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Result.Failure<ModStatus>(error);
        }

        var unfinished = await _journals.LoadAsync(documentsDirectory, cancellationToken) is not null;
        return Result.Success(new ModStatus(record, unfinished, await _backups.ListAsync(documentsDirectory, cancellationToken))
        {
            PrefixGone = ProtonPaths.IsInPrefix(documentsDirectory) && !_fileSystem.Directory.Exists(documentsDirectory),
        });
    }
}
