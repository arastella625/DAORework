namespace DAORework.Core.Mods.Planning;

/// <summary>
/// The dry run: a <see cref="ModActivationPlan"/> as plain lines, for the CLI and the window alike (ROADMAP P2-03).
/// Counts per action and per mod by default, since a texture pack can be thousands of files; every file with
/// <c>listFiles</c>. Blockers and warnings are always listed in full. Mods are named by their ids unless
/// <c>nameOf</c> says otherwise; the window names them by their names (P2.5-18).
/// </summary>
public static class ModActivationPlanText
{
    public static IReadOnlyList<string> Render(ModActivationPlan plan, bool listFiles = false, Func<string, string>? nameOf = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        nameOf ??= id => id;
        var lines = new List<string>
        {
            $"Mods: {Describe(plan.CurrentModIds, nameOf)} -> {Describe(plan.TargetModIds, nameOf)}",
        };

        if (plan.IsEmpty)
        {
            lines.Add(plan.IsBlocked ? "No changes can be planned until the problems below are resolved." : "Nothing to change: the folder already matches.");
        }
        else
        {
            Summary(plan, lines, nameOf);
            PerMod(plan, lines, listFiles, nameOf);
        }

        var warnings = ModActivationPlanWarnings.For(plan, nameOf);
        if (warnings.Count > 0)
        {
            lines.Add("Warnings:");
            lines.AddRange(warnings.Select(w => $"  ! {w}"));
        }

        if (plan.IsBlocked)
        {
            lines.Add("Blocked; nothing will be changed:");
            lines.AddRange(plan.Blockers.Select(b => $"  x {b.Message}"));
        }

        return lines;
    }

    private static void Summary(ModActivationPlan plan, List<string> lines, Func<string, string> nameOf)
    {
        lines.Add($"  Copy      {Files(plan.Count(PlanAction.Copy))}");
        lines.Add($"  Replace   {Files(plan.Count(PlanAction.Replace))}");
        lines.Add($"  Delete    {Files(plan.Count(PlanAction.Delete))}");
        if (plan.AdoptedInPlaceCount > 0)
        {
            lines.Add($"  Adopt     {Files(plan.AdoptedInPlaceCount)} where they are (recorded, not changed)");
        }

        if (plan.Count(PlanAction.Move) > 0)
        {
            lines.Add($"  Move      {Files(plan.Count(PlanAction.Move))} (renamed within the documents folder)");
        }

        lines.Add($"  Unchanged {Files(plan.UnchangedFileCount)}");
        foreach (var change in plan.AddInsChanges)
        {
            lines.Add($"  AddIns.xml: {(change.Add ? "add" : "remove")} {change.Uid} ({nameOf(change.ModId)})");
        }

        lines.Add($"  Writes {plan.BytesToWrite}; that much must be free on the documents folder's drive.");
    }

    private static void PerMod(ModActivationPlan plan, List<string> lines, bool listFiles, Func<string, string> nameOf)
    {
        lines.Add("By mod:");
        foreach (var mod in plan.Operations.GroupBy(o => o.ModId, StringComparer.OrdinalIgnoreCase))
        {
            var counts = mod.GroupBy(o => o.Action).OrderBy(g => g.Key).Select(g => $"{Verb(g.Key)} {g.Count()}");
            lines.Add($"  {nameOf(mod.Key)}: {string.Join(", ", counts)}");
            if (listFiles)
            {
                lines.AddRange(mod.Select(o => o.Action == PlanAction.Move
                    ? $"    {Symbol(o.Action)} {o.MovedFrom} -> {o.TargetPath}"
                    : $"    {Symbol(o.Action)} {o.TargetPath}"));
            }
        }
    }

    private static string Describe(IReadOnlyList<string> mods, Func<string, string> nameOf) =>
        mods.Count == 0 ? "no mods" : $"{mods.Count} ({string.Join(", ", mods.Select(nameOf))})";

    private static string Files(int count) => ModActivationPlanWarnings.Files(count);

    private static string Verb(PlanAction action) => action switch
    {
        PlanAction.Copy => "copy",
        PlanAction.Replace => "replace",
        PlanAction.Move => "move",
        _ => "delete",
    };

    private static char Symbol(PlanAction action) => action switch
    {
        PlanAction.Copy => '+',
        PlanAction.Replace => '~',
        PlanAction.Move => '>',
        _ => '-',
    };
}
