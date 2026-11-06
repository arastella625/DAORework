using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>
/// Leaving mods out of a change: which ones it takes for the rest to go ahead, and how the window says so. Mods are only
/// ever left out of what the player picked, so nothing already on is touched.
/// </summary>
internal static class LeftOut
{
    private const int ReasonsShown = 3;

    /// <returns>The picked mods to leave out; none when that wouldn't clear every blocker or would leave nothing to turn on.</returns>
    public static IReadOnlyList<LibraryModRow> Of(ModActivationPlan plan, IReadOnlyList<LibraryModRow> picked)
    {
        var ids = Ids(plan, [.. picked.Select(m => m.Id)]);
        return [.. picked.Where(m => ids.Contains(m.Id, StringComparer.OrdinalIgnoreCase))];
    }

    /// <returns>The ids of the picked mods to leave out, by the same rule.</returns>
    public static IReadOnlyList<string> Ids(ModActivationPlan plan, IReadOnlyList<string> picked)
    {
        var ids = plan.Blockers.SelectMany(b => b.LeaveOutModIds).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pickedIds = picked.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var clearsAll = plan.Blockers.All(b => b.LeaveOutModIds.Count > 0 && b.LeaveOutModIds.All(pickedIds.Contains));
        var mods = picked.Where(ids.Contains).ToList();
        return clearsAll && mods.Count < picked.Count ? mods : [];
    }

    public static string Label(IReadOnlyList<string> titles) =>
        titles.Count == 1 ? $"Leave out {titles[0]}" : $"Leave out {titles.Count} mods";

    /// <summary>What stops a mod, as the preview said it.</summary>
    public static string Why(ModActivationPlan plan, string modId, Func<string, string> nameOf)
    {
        var own = plan.Blockers.Where(b => b.LeaveOutModIds.Contains(modId, StringComparer.OrdinalIgnoreCase)).ToList();
        var problems = PlanProblems.For(plan with { Blockers = own }, nameOf);
        return problems.Count == 0 ? string.Empty : problems[0].Title;
    }

    /// <summary>Says in a preview who was left out and why, with the reasons of several behind <i>Show details</i>.</summary>
    /// <param name="preview">The preview.</param>
    /// <param name="mods">Who was left out.</param>
    /// <param name="where">Where they stay, such as <i>in your Library, off</i>.</param>
    public static void Show(PlanPreview preview, IReadOnlyList<LeftOutMod> mods, string where)
    {
        if (mods.Count == 0)
        {
            return;
        }

        var names = PlanWords.Names(mods.Select(m => m.Title), title => title);
        preview.Notes.Insert(0, mods.Count == 1
            ? string.Join(' ', new[] { $"Left out: {names}.", mods[0].Why, $"It stays {where}." }.Where(part => part.Length > 0))
            : $"Left out: {names}. They stay {where}.");
        if (mods.Count > 1)
        {
            foreach (var (mod, place) in mods.Select((m, i) => (m, i)))
            {
                preview.Lines.Insert(place, $"Left out {mod.Title}: {mod.Why}");
            }
        }
    }

    /// <summary>What a batch did and what still needs the player, which stays until they've read it (docs/VOICE.md rule 4).</summary>
    /// <param name="done">What it did, as a sentence.</param>
    /// <param name="mods">Who it went on without.</param>
    public static string Outcome(string done, IReadOnlyList<LeftOutMod> mods)
    {
        if (mods.Count == 1)
        {
            return $"{done} {mods[0].Title} needs a look: {mods[0].Why}";
        }

        var reasons = string.Join(" ", mods.Take(ReasonsShown).Select(m => $"{m.Title}: {m.Why}"));
        var more = mods.Count > ReasonsShown ? $" And {mods.Count - ReasonsShown} more." : string.Empty;
        return $"{done} {mods.Count} need a look. {reasons}{more}";
    }
}
