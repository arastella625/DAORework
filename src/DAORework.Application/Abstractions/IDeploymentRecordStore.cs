using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Abstractions;

/// <summary>The deployment record of each documents folder (ADR-0012), under app-data. Written atomically.</summary>
public interface IDeploymentRecordStore
{
    /// <summary>The folder's record; the empty record when DAORework has never deployed there.</summary>
    Task<Result<DeploymentRecord>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default);

    Task SaveAsync(string documentsDirectory, DeploymentRecord record, CancellationToken cancellationToken = default);
}
