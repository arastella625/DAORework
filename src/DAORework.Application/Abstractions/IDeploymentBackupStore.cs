using DAORework.Core.Common;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Abstractions;

/// <summary>
/// DAORework's store of backups of mod changes (ROADMAP P2-04), one folder per documents folder under app-data. Saving
/// checks every file against the hash the plan expects and verifies each stored copy; restoring verifies every file it
/// puts back. It reads the documents folder when saving and writes it only when restoring.
/// </summary>
public interface IDeploymentBackupStore
{
    /// <summary>Captures <paramref name="targets"/>, with the deployment record from before the change. A failure leaves no backup behind.</summary>
    Task<Result<DeploymentBackup>> SaveAsync(
        string documentsDirectory,
        IReadOnlyList<BackupTarget> targets,
        DeploymentRecord? previousRecord = null,
        CancellationToken cancellationToken = default);

    /// <summary>The documents folder's complete backups, newest first.</summary>
    Task<IReadOnlyList<DeploymentBackup>> ListAsync(string documentsDirectory, CancellationToken cancellationToken = default);

    /// <summary>Returns every path in the backup to what it held before, verified by hash.</summary>
    Task<Result<RestoreReport>> RestoreAsync(DeploymentBackup backup, CancellationToken cancellationToken = default);
}
