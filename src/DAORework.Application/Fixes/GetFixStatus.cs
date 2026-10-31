using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Fixes;

namespace DAORework.Application.Fixes;

/// <summary>
/// Reads which fixes DAORework applied in an install folder and checks each file against the record, so a fix Steam's
/// verify has undone shows as no longer in effect (ADR-0013). Reads only.
/// </summary>
public sealed class GetFixStatus
{
    private readonly IDeploymentRecordStore _records;
    private readonly IApplyJournalStore _journals;
    private readonly IDeploymentBackupStore _backups;
    private readonly IFixLibrary _library;
    private readonly IInstallFiles _files;

    public GetFixStatus(IDeploymentRecordStore records, IApplyJournalStore journals, IDeploymentBackupStore backups, IFixLibrary library, IInstallFiles files)
    {
        _records = records;
        _journals = journals;
        _backups = backups;
        _library = library;
        _files = files;
    }

    public async Task<Result<FixStatus>> ExecuteAsync(string installDirectory, CancellationToken cancellationToken = default)
    {
        if (!(await _records.LoadAsync(installDirectory, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Result.Failure<FixStatus>(error);
        }

        if (!(await _files.HashAsync(installDirectory, record.Files.Select(f => f.TargetPath).ToList(), cancellationToken)).TryGetValue(out var hashes, out error))
        {
            return Result.Failure<FixStatus>(error);
        }

        var library = await _library.ListAsync(cancellationToken);
        var applied = record.ActiveModIds.Select(id =>
        {
            var entry = library.FirstOrDefault(e => e.LibraryId.Equals(id, StringComparison.OrdinalIgnoreCase));
            var changed = record.Files
                .Where(f => f.ModId.Equals(id, StringComparison.OrdinalIgnoreCase))
                .Where(f => !hashes.TryGetValue(f.TargetPath, out var sha) || !sha.Equals(f.Sha256, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.TargetPath)
                .ToList();
            return new AppliedFix(id, entry is null ? id : FixIds.DisplayName(entry.Fix), entry?.Version, changed);
        }).ToList();

        var unfinished = await _journals.LoadAsync(installDirectory, cancellationToken) is not null;
        return Result.Success(new FixStatus(applied, unfinished, await _backups.ListAsync(installDirectory, cancellationToken)));
    }
}
