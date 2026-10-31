using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Fixes;

/// <summary>A fix plan as the lines the CLI shows before asking (ADR-0013): each file, what happens to it, and any blocker.</summary>
public static class FixPlanText
{
    public static IReadOnlyList<string> Render(FixPlan plan, string fixName)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var change = plan.Change;
        var lines = new List<string> { $"{(plan.Kind == FixChangeKind.Apply ? "Apply" : "Remove")}: {fixName} ({plan.EntryId})" };
        if (change.IsEmpty)
        {
            lines.Add(change.IsBlocked ? "No changes can be planned until the problems below are resolved." : "Nothing to change: the install folder already matches.");
        }

        lines.AddRange(change.Operations.Select(o => $"  {Verb(plan.Kind, o.Action)} {o.TargetPath}"));
        if (change.UnchangedFileCount > 0)
        {
            lines.Add($"  Already in place: {change.UnchangedFileCount} file(s)");
        }

        lines.AddRange(plan.ChangedSince.Select(p => $"  Changed outside DAORework since, so left in place: {p}"));
        lines.AddRange(change.MissingRecordedFiles.Select(p => $"  Missing since DAORework placed it: {p}"));
        if (change.BytesToWrite.Bytes > 0)
        {
            lines.Add($"  Writes {change.BytesToWrite}; the files it replaces are backed up first.");
        }

        if (change.IsBlocked)
        {
            lines.Add("Blocked; nothing will be changed:");
            lines.AddRange(change.Blockers.Select(b => $"  x {b.Message}"));
        }

        return lines;
    }

    private static string Verb(FixChangeKind kind, PlanAction action) => action switch
    {
        PlanAction.Copy => "Add",
        PlanAction.Delete => "Delete",
        _ => kind == FixChangeKind.Apply ? "Replace" : "Put back the original",
    };
}
