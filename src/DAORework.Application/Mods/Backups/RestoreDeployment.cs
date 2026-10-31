using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;

namespace DAORework.Application.Mods.Backups;

/// <summary>
/// Puts a documents folder back as it was before a plan ran, from the plan's backup (ROADMAP P2-04). The executor
/// (P2-05) calls it when an apply cannot finish; the rollback command (P2-06) calls it on request.
/// </summary>
public sealed class RestoreDeployment
{
    private readonly IDeploymentBackupStore _store;

    public RestoreDeployment(IDeploymentBackupStore store)
    {
        _store = store;
    }

    public async Task<Result<RestoreReport>> ExecuteAsync(RestoreDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var backups = await _store.ListAsync(request.DocumentsDirectory, cancellationToken);
        var backup = request.BackupId is null
            ? (backups.Count > 0 ? backups[0] : null)
            : backups.FirstOrDefault(b => string.Equals(b.Id, request.BackupId, StringComparison.OrdinalIgnoreCase));
        if (backup is null)
        {
            var which = request.BackupId is null ? "backup" : $"backup {request.BackupId}";
            return Result.Failure<RestoreReport>(new Error(DiagnosticCodes.ModBackupNotFound, $"There is no {which} of changes for {request.DocumentsDirectory}."));
        }

        return await _store.RestoreAsync(backup, cancellationToken);
    }
}
