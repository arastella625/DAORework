using System.Globalization;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// The dry run: a <see cref="ModActivationPlan"/> as plain lines, for the CLI and the window alike (ROADMAP P2-03).
/// Counts per action and per mod by default, since a texture pack can be thousands of files; every file with
/// <c>listFiles</c>. Blockers and warnings are always listed in full.
/// </summary>
public static class ModActivationPlanText
{
    public static IReadOnlyList<string> Render(ModActivationPlan plan, bool listFiles = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var lines = new List<string>
        {
            $"Mods: {Describe(plan.CurrentModIds)} -> {Describe(plan.TargetModIds)}",
        };

        if (plan.IsEmpty)
        {
            lines.Add(plan.IsBlocked ? "No changes can be planned until the problems below are resolved." : "Nothing to change: the folder already matches.");
        }
        else
        {
            Summary(plan, lines);
            PerMod(plan, lines, listFiles);
        }

        Warnings(plan, lines);
        if (plan.IsBlocked)
        {
            lines.Add("Blocked; nothing will be changed:");
            lines.AddRange(plan.Blockers.Select(b => $"  x {b.Message}"));
        }

        return lines;
    }

    private static void Summary(ModActivationPlan plan, List<string> lines)
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
            lines.Add($"  AddIns.xml: {(change.Add ? "add" : "remove")} {change.Uid} ({change.ModId})");
        }

        lines.Add($"  Writes {plan.BytesToWrite}; that much must be free on the documents folder's drive.");
    }

    private static void PerMod(ModActivationPlan plan, List<string> lines, bool listFiles)
    {
        lines.Add("By mod:");
        foreach (var mod in plan.Operations.GroupBy(o => o.ModId, StringComparer.OrdinalIgnoreCase))
        {
            var counts = mod.GroupBy(o => o.Action).OrderBy(g => g.Key).Select(g => $"{Verb(g.Key)} {g.Count()}");
            lines.Add($"  {mod.Key}: {string.Join(", ", counts)}");
            if (listFiles)
            {
                lines.AddRange(mod.Select(o => o.Action == PlanAction.Move
                    ? $"    {Symbol(o.Action)} {o.MovedFrom} -> {o.TargetPath}"
                    : $"    {Symbol(o.Action)} {o.TargetPath}"));
            }
        }
    }

    private static void Warnings(ModActivationPlan plan, List<string> lines)
    {
        var conflicts = plan.Conflicts;
        var warnings = conflicts.OverrideConflicts.Select(c =>
                $"{c.FileName} is in {string.Join(", ", c.Overridden.Select(o => o.ModId))} and {c.Winner.ModId}; {c.Winner.ModId}'s copy is used{(c.SameContent ? " (identical content)" : string.Empty)}.")
            .Concat(conflicts.UnmanagedShadows.Select(s =>
                $"{s.Managed.FileName} from {s.Managed.ModId} has the same name as {string.Join(", ", s.UnmanagedPaths)} in the override folder, which DAORework does not manage; the game picks one."))
            .Concat(conflicts.AddInFilesOverridden.Select(a =>
                $"{a.Override.FileName} from {a.Override.ModId} replaces the add-in file {a.AddInFile.TargetPath} in the game."))
            .ToList();
        foreach (var mod in conflicts.ProgramFiles.GroupBy(f => f.ModId, StringComparer.OrdinalIgnoreCase))
        {
            var names = string.Join(", ", mod.Select(f => f.FileName));
            var dxvk = mod.Any(f => f.FileName.Equals("d3d9.dll", StringComparison.OrdinalIgnoreCase)) ? " For DXVK, use: fixes apply dxvk <release>." : string.Empty;
            warnings.Add($"{mod.Key} holds {Files(mod.Count())} that {(mod.Count() == 1 ? "is a program" : "are programs")} ({names}); programs are never installed into the game's folders.{dxvk}");
        }

        if (plan.MissingRecordedFiles.Count > 0)
        {
            var count = plan.MissingRecordedFiles.Count;
            warnings.Add($"{Files(count)} DAORework placed earlier {(count == 1 ? "is" : "are")} missing; any still needed are copied again.");
        }

        if (warnings.Count > 0)
        {
            lines.Add("Warnings:");
            lines.AddRange(warnings.Select(w => $"  ! {w}"));
        }
    }

    private static string Describe(IReadOnlyList<string> mods) =>
        mods.Count == 0 ? "no mods" : $"{mods.Count} ({string.Join(", ", mods)})";

    private static string Files(int count) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? "file" : "files")}");

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
