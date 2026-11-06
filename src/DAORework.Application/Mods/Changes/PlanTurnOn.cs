using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Changes;

/// <summary>
/// Plans turning mods on or off, taking over identical files already in the game (ADR-0037), unless a live Vortex would
/// put them back. When Vortex's records can't be read, nothing is taken over: the plan copies, as it always did.
/// </summary>
public sealed class PlanTurnOn
{
    private readonly PlanModChange _plan;
    private readonly IHandInstalledModsReader _reader;

    public PlanTurnOn(PlanModChange plan, IHandInstalledModsReader reader)
    {
        _plan = plan;
        _reader = reader;
    }

    public async Task<Result<ModActivationPlan>> ExecuteAsync(PlanModChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await _plan.ExecuteAsync(request, cancellationToken);
        if (!result.TryGetValue(out var plan, out _) || plan.SameUnmanagedCopies.Count == 0)
        {
            return result;
        }

        if (!(await _reader.ReadAsync(request.DocumentsDirectory, cancellationToken)).TryGetValue(out var snapshot, out _) || snapshot.VortexDeployedOverride)
        {
            return result;
        }

        var live = snapshot.VortexRecords
            .SelectMany(r => r.Files.Where(f => r.IsLive(f.Mod)).Select(f => f.Path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Result.Success(TakeOverCopies.Apply(plan, live));
    }
}
