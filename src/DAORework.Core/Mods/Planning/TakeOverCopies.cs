using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// Turning a mod on takes over identical files already in the game (ADR-0037): a copy becomes a move of the file that is
/// there, and any further identical copy is deleted with a backup, so no duplicate is made. Each hash is checked again
/// when the step runs. Pure.
/// </summary>
public static class TakeOverCopies
{
    private static readonly StringComparer _paths = StringComparer.OrdinalIgnoreCase;

    /// <param name="plan">The plan, with the files that are the same as a mod's.</param>
    /// <param name="leaveAlone">Documents-relative paths not to touch: what a live Vortex would put back (ADR-0028 §2).</param>
    public static ModActivationPlan Apply(ModActivationPlan plan, IReadOnlySet<string> leaveAlone)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(leaveAlone);
        var operations = plan.Operations.ToList();
        var taken = new HashSet<string>(_paths);
        foreach (var shadow in plan.SameUnmanagedCopies)
        {
            var copy = operations.FindIndex(o => o.Action == PlanAction.Copy && _paths.Equals(o.TargetPath, shadow.Managed.TargetPath));
            if (copy < 0)
            {
                continue;
            }

            var found = shadow.UnmanagedPaths
                .Select(p => ModLayout.OverridePrefix + p.Replace('\\', '/'))
                .Where(p => !leaveAlone.Contains(p) && taken.Add(p))
                .ToList();
            if (found.Count == 0)
            {
                continue;
            }

            var (target, mod, sha) = (shadow.Managed.TargetPath, shadow.Managed.ModId, shadow.Managed.Source.Sha256);
            operations[copy] = new PlanOperation(PlanAction.Move, target, mod, null, sha) { MovedFrom = found[0] };
            operations.AddRange(found.Skip(1).Select(p => new PlanOperation(PlanAction.Delete, p, mod, null, sha)));
        }

        return plan with { Operations = [.. operations.OrderBy(o => o.Action).ThenBy(o => o.TargetPath, _paths)] };
    }
}
