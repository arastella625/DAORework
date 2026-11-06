namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// What a mod's row says about the files it shares (P2.6-33): whether its copies are used, lost or both. Only copies that
/// differ count, since identical ones change nothing, and only override files, where the winner is known. Pure.
/// </summary>
public static class ConflictMarks
{
    /// <summary>Each mod with a differing shared file, by its id, and its mark; a mod with none is not listed.</summary>
    public static IReadOnlyDictionary<string, string> For(IReadOnlyList<OverrideConflict> conflicts)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        var differing = conflicts.Where(c => !c.SameContent).ToList();
        var wins = differing.Select(c => c.Winner.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var loses = differing.SelectMany(c => c.OtherModIds).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return wins.Union(loses, StringComparer.OrdinalIgnoreCase).ToDictionary(
            id => id,
            id => (wins.Contains(id), loses.Contains(id)) switch
            {
                (true, true) => "wins and loses",
                (true, false) => "wins",
                _ => "loses",
            },
            StringComparer.OrdinalIgnoreCase);
    }
}
