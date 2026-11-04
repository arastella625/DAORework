using DAORework.Core.Common;
using DAORework.Core.Mods.Handover;

namespace DAORework.Application.Abstractions;

/// <summary>
/// What the handover from Vortex keeps for each documents folder (ADR-0028 §3), under app-data, since the purge deletes
/// Vortex's own record. Written atomically. Nothing of Vortex's is ever written.
/// </summary>
public interface IVortexHandoverStore
{
    /// <summary>The kept handover, or null when none was started.</summary>
    Task<Result<VortexHandover?>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default);

    Task SaveAsync(string documentsDirectory, VortexHandover handover, CancellationToken cancellationToken = default);

    /// <summary>Keeps <c>Settings\AddIns.xml</c> as it is now, if it is there and none is kept yet, for the last step.</summary>
    Task KeepAddInsListAsync(string documentsDirectory, CancellationToken cancellationToken = default);
}
