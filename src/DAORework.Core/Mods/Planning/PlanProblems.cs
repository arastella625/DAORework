namespace DAORework.Core.Mods.Planning;

/// <summary>
/// A plan's blockers as the window shows them, first thing in a preview: one problem for each cause rather than one line
/// for each file, however many files it touches, so turning on dozens of mods still reads in one screen. The file-by-file
/// list stays in <see cref="ModActivationPlanText"/>. Pure.
/// </summary>
public static class PlanProblems
{
    private const int MaxShown = 5;

    /// <param name="plan">The plan.</param>
    /// <param name="nameOf">How a mod is named.</param>
    public static IReadOnlyList<PlanProblem> For(ModActivationPlan plan, Func<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(nameOf);
        var problems = All(plan, nameOf);
        if (problems.Count <= MaxShown)
        {
            return problems;
        }

        var more = problems.Count - (MaxShown - 1);
        return [.. problems.Take(MaxShown - 1), new PlanProblem(PlanWords.Count(more, "more problem", "more problems"), "Show details lists them.")];
    }

    /// <summary>How many causes there are, including any past the rows shown.</summary>
    public static int Count(ModActivationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return All(plan, id => id).Count;
    }

    private static List<PlanProblem> All(ModActivationPlan plan, Func<string, string> nameOf) =>
        [.. plan.Blockers.GroupBy(b => b.Kind).OrderBy(g => g.Key).SelectMany(g => Describe(g.Key, [.. g], plan, nameOf))];

    private static IEnumerable<PlanProblem> Describe(PlanBlockerKind kind, List<PlanBlocker> blockers, ModActivationPlan plan, Func<string, string> nameOf) => kind switch
    {
        PlanBlockerKind.PathCollision => SameFiles(blockers, nameOf),
        PlanBlockerKind.GameProgramNotInstalled => GamePrograms(blockers, plan, nameOf),
        PlanBlockerKind.UnmanagedFileInTheWay => [InTheWay(blockers, nameOf)],
        PlanBlockerKind.ChangedSinceDeployed => [Changed(blockers.Count)],
        PlanBlockerKind.AddInAlreadyRegistered => [Registered(blockers, nameOf)],
        _ => blockers.Select(b => new PlanProblem(b.Message, string.Empty)),
    };

    /// <summary>One problem for each set of mods, which is what the player chooses between.</summary>
    private static IEnumerable<PlanProblem> SameFiles(List<PlanBlocker> blockers, Func<string, string> nameOf) =>
        blockers.GroupBy(b => string.Join('\n', b.ModIds), StringComparer.OrdinalIgnoreCase).Select(set =>
            new PlanProblem(
                $"{PlanWords.Names(set.First().ModIds, nameOf)} would write {ModActivationPlanWarnings.Files(set.Count())} to the same place, so they can't be on together.",
                "Only one of them can be on."));

    /// <summary>A mod that is off can be left out; one turned on before DAORework knew better has to be turned off.</summary>
    private static List<PlanProblem> GamePrograms(List<PlanBlocker> blockers, ModActivationPlan plan, Func<string, string> nameOf)
    {
        var mods = blockers.SelectMany(b => b.ModIds).ToList();
        var on = mods.Where(id => plan.CurrentModIds.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();
        var off = mods.Except(on, StringComparer.OrdinalIgnoreCase).ToList();
        var problems = new List<PlanProblem>();
        if (off.Count > 0)
        {
            problems.Add(new PlanProblem(
                $"{PlanWords.Names(off, nameOf)} {(off.Count == 1 ? "runs" : "run")} inside the game, and DAORework can't install that kind of mod yet.",
                $"{(off.Count == 1 ? "It stays" : "They stay")} in your Library, off.{Instead(blockers)}"));
        }

        if (on.Count > 0)
        {
            problems.Add(new PlanProblem(
                $"{PlanWords.Names(on, nameOf)} {(on.Count == 1 ? "runs" : "run")} inside the game, so {(on.Count == 1 ? "its" : "their")} files do nothing where they are.",
                $"Turn {It(on.Count)} off first."));
        }

        return problems;
    }

    private static PlanProblem InTheWay(List<PlanBlocker> blockers, Func<string, string> nameOf)
    {
        var count = blockers.Count;
        var mods = blockers.SelectMany(b => b.ModIds).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new PlanProblem(
            $"{ModActivationPlanWarnings.Files(count)} from {PlanWords.Names(mods, nameOf)} {(count == 1 ? "is" : "are")} already in your game, and DAORework didn't put {It(count)} there.",
            $"DAORework never overwrites those. Take the other copy out of your game first, or go without {(mods.Count == 1 ? "this mod" : "these mods")}.");
    }

    private static PlanProblem Changed(int count) => new(
        $"{ModActivationPlanWarnings.Files(count)} DAORework placed {(count == 1 ? "was" : "were")} changed afterwards, so DAORework won't touch {It(count)}.",
        "Put the original back or delete the changed copy, then try again. Show details lists which.");

    private static PlanProblem Registered(List<PlanBlocker> blockers, Func<string, string> nameOf)
    {
        var mods = blockers.SelectMany(b => b.ModIds).ToList();
        return new PlanProblem(
            $"{PlanWords.Names(mods, nameOf)} {(mods.Count == 1 ? "is" : "are")} already installed some other way.",
            $"Uninstall the other copy first, or go without {It(mods.Count)}.");
    }

    private static string Instead(List<PlanBlocker> blockers) =>
        blockers.Select(b => b.Instead).OfType<string>().Distinct(StringComparer.Ordinal).FirstOrDefault() is { } instead ? " " + instead : string.Empty;

    private static string It(int count) => count == 1 ? "it" : "them";
}
