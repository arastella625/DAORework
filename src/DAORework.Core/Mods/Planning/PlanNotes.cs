namespace DAORework.Core.Mods.Planning;

/// <summary>
/// What a preview warns about (VOICE.md): the sentences of <see cref="ModActivationPlanWarnings"/> while they fit, and
/// a few grouped ones once a plan is too big for that, so a large plan reads in one screen. Identical copies of a file
/// aren't mentioned, since nothing depends on them. Every file stays in <see cref="ModActivationPlanText"/>. Pure.
/// </summary>
public static class PlanNotes
{
    private const int MaxSentences = 6;

    /// <param name="plan">The plan.</param>
    /// <param name="nameOf">How a mod is named.</param>
    public static IReadOnlyList<string> For(ModActivationPlan plan, Func<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(nameOf);
        nameOf = ModActivationPlanWarnings.CombinedAware(nameOf);
        var differing = plan.Conflicts.BetweenMods.Where(c => !c.SameContent).ToList();
        var sentences = differing.Select(c => ModActivationPlanWarnings.Shared(c, nameOf)).Concat(ModActivationPlanWarnings.Others(plan, nameOf)).ToList();
        return sentences.Count <= MaxSentences ? sentences : [.. SharedFileNotes.For(differing, nameOf), .. Grouped(plan, nameOf)];
    }

    private static List<string> Grouped(ModActivationPlan plan, Func<string, string> nameOf)
    {
        var conflicts = plan.Conflicts;
        var notes = new List<string>();
        if (conflicts.UnmanagedShadows.Count > 0)
        {
            var mods = conflicts.UnmanagedShadows.Select(s => s.Managed.ModId);
            notes.Add($"{ModActivationPlanWarnings.Files(conflicts.UnmanagedShadows.Count)} from {PlanWords.Names(mods, nameOf)} {Verb(conflicts.UnmanagedShadows.Count, "shares", "share")} a name with files in the override folder that DAORework doesn't manage; the game picks one.");
        }

        if (conflicts.AddInFilesOverridden.Count > 0)
        {
            var mods = conflicts.AddInFilesOverridden.Select(a => a.Override.ModId);
            notes.Add($"{ModActivationPlanWarnings.Files(conflicts.AddInFilesOverridden.Count)} from {PlanWords.Names(mods, nameOf)} {Verb(conflicts.AddInFilesOverridden.Count, "replaces a file", "replace files")} inside add-ins.");
        }

        if (conflicts.ProgramFiles.Count > 0)
        {
            var mods = conflicts.ProgramFiles.Select(f => f.ModId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            notes.Add($"{PlanWords.Names(mods, nameOf)} {Verb(mods.Count, "holds", "hold")} programs, which are never installed into the game's folders.");
        }

        if (plan.MissingRecordedFiles.Count > 0)
        {
            notes.Add(ModActivationPlanWarnings.Missing(plan.MissingRecordedFiles.Count));
        }

        notes.AddRange(plan.CombineProblems);

        var restored = plan.AddInsChanges.Count(c => c.Item is not null);
        if (restored > 0)
        {
            notes.Add($"Puts {PlanWords.Count(restored, "add-in", "add-ins")} back in AddIns.xml: the purge in Vortex took {(restored == 1 ? "it" : "them")} out.");
        }

        if (plan.InstallOverrideShadows.Count > 0)
        {
            notes.Add($"{ModActivationPlanWarnings.Files(plan.InstallOverrideShadows.Count)} {Verb(plan.InstallOverrideShadows.Count, "is", "are")} also in your game folder's override, which DAORework doesn't manage; the game uses one copy.");
        }

        if (plan.InstallOverrideProblem is { } unread)
        {
            notes.Add(unread);
        }

        return notes;
    }

    private static string Verb(int count, string one, string many) => count == 1 ? one : many;
}
