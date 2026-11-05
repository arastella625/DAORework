using System.Globalization;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;

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
        return [.. plan.Conflicts.BetweenMods.Select(c => Shared(c, nameOf)), .. Others(plan, nameOf)];
    }

    /// <summary>The sentence for one file two mods ship.</summary>
    internal static string Shared(OverrideConflict c, Func<string, string> nameOf) =>
        $"{c.FileName} is in {string.Join(", ", c.OtherModIds.Select(nameOf))} and {nameOf(c.Winner.ModId)}; {nameOf(c.Winner.ModId)}'s copy is used{(c.SameContent ? " (identical content)" : string.Empty)}.{Meaning(c)}";

    /// <summary>Every warning that isn't about files two mods ship.</summary>
    internal static IReadOnlyList<string> Others(ModActivationPlan plan, Func<string, string> nameOf)
    {
        var conflicts = plan.Conflicts;
        var warnings = conflicts.UnmanagedShadows.Select(s =>
                $"{s.Managed.FileName} from {nameOf(s.Managed.ModId)} has the same name as {string.Join(", ", s.UnmanagedPaths)} in the override folder, which DAORework does not manage; the game picks one.")
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
            warnings.Add(Missing(plan.MissingRecordedFiles.Count));
        }

        if (Tidied(plan) is { } tidied)
        {
            warnings.Add(tidied);
        }

        warnings.AddRange(plan.CombineProblems);
        warnings.AddRange(plan.AddInsChanges.Where(c => c.Item is not null).Select(c => $"Puts {c.ModId} back in AddIns.xml: the purge in Vortex took it out."));
        warnings.AddRange(InstallOverrideWarnings.For(plan, nameOf));
        return warnings;
    }

    /// <summary>Says when a mod that stays on loses files placed before they were left out (ADR-0036), which would otherwise look like a surprise.</summary>
    internal static string? Tidied(ModActivationPlan plan)
    {
        var staying = plan.TargetModIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var count = plan.Operations.Count(o => o.Action == PlanAction.Delete && staying.Contains(o.ModId) && NonModFiles.Is(o.TargetPath));
        return count == 0 ? null : $"Takes {Files(count)} that {(count == 1 ? "means" : "mean")} nothing to the game, such as readmes, out of your game. Your Library keeps {(count == 1 ? "it" : "them")}.";
    }

    internal static string Missing(int count) =>
        $"{Files(count)} DAORework placed earlier {(count == 1 ? "is" : "are")} missing; any still needed are copied again.";

    /// <summary>What sharing the file means, by its kind (P2.5-21); nothing for identical copies.</summary>
    private static string Meaning(OverrideConflict conflict) =>
        !conflict.SameContent && SharedFileMeaning.For(conflict.FileName) is { } meaning ? " " + meaning : string.Empty;

    /// <summary>Names the combined files' record id in words (ADR-0025), and every other id as <paramref name="nameOf"/> does.</summary>
    public static Func<string, string> CombinedAware(Func<string, string> nameOf) =>
        id => string.Equals(id, CombinedFiles.ModId, StringComparison.OrdinalIgnoreCase) ? CombinedFiles.DisplayName : nameOf(id);

    internal static string Files(int count) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? "file" : "files")}");
}
