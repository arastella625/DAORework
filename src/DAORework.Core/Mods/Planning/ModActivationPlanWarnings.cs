using System.Globalization;
using DAORework.Core.Mods.Combining;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// What a <see cref="ModActivationPlan"/> warns about, as sentences: files two mods ship, files that share a name with
/// the user's own override files, add-in files an override replaces, programs left out, and files DAORework placed
/// that are gone. Shared by the plan's text (the CLI, the window's details) and the window's summary (P2.5-18).
/// </summary>
public static class ModActivationPlanWarnings
{
    /// <param name="plan">The plan.</param>
    /// <param name="nameOf">How a mod is named: by its id in the CLI, by its name in the window.</param>
    public static IReadOnlyList<string> For(ModActivationPlan plan, Func<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(nameOf);
        nameOf = CombinedAware(nameOf);
        var conflicts = plan.Conflicts;
        var warnings = conflicts.OverrideConflicts.Select(c =>
                $"{c.FileName} is in {string.Join(", ", c.Overridden.Select(o => nameOf(o.ModId)))} and {nameOf(c.Winner.ModId)}; {nameOf(c.Winner.ModId)}'s copy is used{(c.SameContent ? " (identical content)" : string.Empty)}.")
            .Concat(conflicts.UnmanagedShadows.Select(s =>
                $"{s.Managed.FileName} from {nameOf(s.Managed.ModId)} has the same name as {string.Join(", ", s.UnmanagedPaths)} in the override folder, which DAORework does not manage; the game picks one."))
            .Concat(conflicts.AddInFilesOverridden.Select(a =>
                $"{a.Override.FileName} from {nameOf(a.Override.ModId)} replaces the add-in file {a.AddInFile.TargetPath} in the game."))
            .ToList();
        foreach (var mod in conflicts.ProgramFiles.GroupBy(f => f.ModId, StringComparer.OrdinalIgnoreCase))
        {
            var names = string.Join(", ", mod.Select(f => f.FileName));
            var dxvk = mod.Any(f => f.FileName.Equals("d3d9.dll", StringComparison.OrdinalIgnoreCase)) ? " For DXVK, use: fixes apply dxvk <release>." : string.Empty;
            warnings.Add($"{nameOf(mod.Key)} holds {Files(mod.Count())} that {(mod.Count() == 1 ? "is a program" : "are programs")} ({names}); programs are never installed into the game's folders.{dxvk}");
        }

        if (plan.MissingRecordedFiles.Count > 0)
        {
            var count = plan.MissingRecordedFiles.Count;
            warnings.Add($"{Files(count)} DAORework placed earlier {(count == 1 ? "is" : "are")} missing; any still needed are copied again.");
        }

        warnings.AddRange(plan.CombineProblems);
        return warnings;
    }

    /// <summary>Names the combined files' record id in words (ADR-0025), and every other id as <paramref name="nameOf"/> does.</summary>
    public static Func<string, string> CombinedAware(Func<string, string> nameOf) =>
        id => string.Equals(id, CombinedFiles.ModId, StringComparison.OrdinalIgnoreCase) ? CombinedFiles.DisplayName : nameOf(id);

    internal static string Files(int count) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? "file" : "files")}");
}
