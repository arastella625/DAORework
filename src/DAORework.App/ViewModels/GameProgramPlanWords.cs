using DAORework.Application.Mods.Programs;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>How the preview of a mod that runs inside the game reads (docs/VOICE.md): one line of what it changes, then its notes.</summary>
internal static class GameProgramPlanWords
{
    public static string Summary(FixPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var change = plan.Change;
        if (change.IsBlocked)
        {
            return $"Can't go ahead: {change.Blockers[0].Message}";
        }

        var copies = change.Operations.Count(o => o.Action == PlanAction.Copy);
        var deletes = change.Operations.Count(o => o.Action == PlanAction.Delete);
        return (plan.Kind, copies, deletes) switch
        {
            (FixChangeKind.Apply, > 0, _) => $"Copies {Counted.Of(copies, "file")} into your game folder.",
            (FixChangeKind.Apply, _, _) => "Its files are already in your game folder.",
            (_, _, > 0) => $"Removes {Counted.Of(deletes, "file")} from your game folder.",
            _ => "None of its files are left in your game folder to remove.",
        };
    }

    /// <summary>What the plan noted, and each file left in place because the player changed it, such as an edited <c>.ini</c>.</summary>
    public static IEnumerable<string> Notes(PlannedGameProgramMod planned)
    {
        ArgumentNullException.ThrowIfNull(planned);
        return planned.Notes.Concat(planned.Plan.ChangedSince.Select(path => $"{path} changed after it was placed, so it stays where it is."));
    }
}
