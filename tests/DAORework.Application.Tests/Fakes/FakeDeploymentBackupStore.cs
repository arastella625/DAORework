using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeDeploymentBackupStore : IDeploymentBackupStore
{
    public List<DeploymentBackup> Backups { get; } = [];

    public IReadOnlyList<BackupTarget>? LastTargets { get; private set; }

    public DeploymentBackup? Restored { get; private set; }

    public Task<Result<DeploymentBackup>> SaveAsync(
        string documentsDirectory,
        IReadOnlyList<BackupTarget> targets,
        DeploymentRecord? previousRecord = null,
        CancellationToken cancellationToken = default)
    {
        LastTargets = targets;
        var backup = new DeploymentBackup($"b{Backups.Count}", DateTimeOffset.UnixEpoch, documentsDirectory, []) { PreviousRecord = previousRecord };
        Backups.Insert(0, backup);
        return Task.FromResult(Result.Success(backup));
    }

    public Task<IReadOnlyList<DeploymentBackup>> ListAsync(string documentsDirectory, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DeploymentBackup>>(Backups.Where(b => b.DocumentsDirectory == documentsDirectory).ToList());

    public Task<Result<RestoreReport>> RestoreAsync(DeploymentBackup backup, CancellationToken cancellationToken = default)
    {
        Restored = backup;
        return Task.FromResult(Result.Success(new RestoreReport(1, 0, [])));
    }
}
