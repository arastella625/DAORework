using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>
/// Leaving mods out of a blocked change: which ones it takes for the rest to go ahead, and how the preview says so. Mods
/// are only ever left out of what the player picked, so nothing already on is touched.
/// </summary>
internal static class LeftOut
{
    /// <returns>The picked mods to leave out; none when that wouldn't clear every blocker or would leave nothing to turn on.</returns>
    public static IReadOnlyList<LibraryModRow> Of(ModActivationPlan plan, IReadOnlyList<LibraryModRow> picked)
    {
        var ids = plan.Blockers.SelectMany(b => b.LeaveOutModIds).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pickedIds = picked.Select(m => m.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var clearsAll = plan.Blockers.All(b => b.LeaveOutModIds.Count > 0 && b.LeaveOutModIds.All(pickedIds.Contains));
        var mods = picked.Where(m => ids.Contains(m.Id)).ToList();
        return clearsAll && mods.Count < picked.Count ? mods : [];
    }

    public static string Label(IReadOnlyList<LibraryModRow> mods) =>
        mods.Count == 1 ? $"Leave out {mods[0].Title}" : $"Leave out {mods.Count} mods";

    public static string Note(IReadOnlyList<LibraryModRow> mods) =>
        $"Left out: {PlanWords.Names(mods.Select(m => m.Title), title => title)}. {(mods.Count == 1 ? "It stays" : "They stay")} in your Library, off.";
}
