using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// Files several mods ship, as a few sentences however many there are: how many, what can go wrong with them
/// (<see cref="SharedFileRank"/>), then one line for each set of mods, biggest first. Pure.
/// </summary>
public static class SharedFileNotes
{
    private const int GroupsShown = 3;

    /// <param name="conflicts">The conflicts between mods whose copies differ.</param>
    /// <param name="nameOf">How a mod is named.</param>
    public static IReadOnlyList<string> For(IReadOnlyList<OverrideConflict> conflicts, Func<string, string> nameOf)
    {
        if (conflicts.Count == 0)
        {
            return [];
        }

        var notes = new List<string>
        {
            conflicts.Count == 1 ? "1 file is in more than one mod. The mod lower in the order wins." : $"{ModActivationPlanWarnings.Files(conflicts.Count)} are in more than one mod. For each one, the mod lower in the order wins.",
        };
        var ranked = SharedFileRank.Summary([.. conflicts.Select(c => c.FileName)]);
        notes.Add(conflicts.Any(IsBehaviour) ? $"{ranked} For what could break quests, look for a compatibility patch." : ranked ?? string.Empty);

        var groups = Groups(conflicts, nameOf);
        notes.AddRange(groups.Take(GroupsShown).Select(g => $"{g.Title}: {g.Kinds}."));
        if (groups.Count > GroupsShown)
        {
            notes.Add($"{PlanWords.Count(groups.Count - GroupsShown, "more set of mods shares", "more sets of mods share")} files. Show details lists them.");
        }

        return notes;
    }

    /// <summary>One group for each set of mods, those that lose behaviour first, then the biggest.</summary>
    public static IReadOnlyList<SharedFileGroup> Groups(IReadOnlyList<OverrideConflict> conflicts, Func<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(nameOf);
        return [.. conflicts
            .GroupBy(c => $"{c.Winner.ModId}\n{string.Join('\n', c.OtherModIds.Order(StringComparer.OrdinalIgnoreCase))}", StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Any(IsBehaviour))
            .ThenByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => Group([.. g], nameOf))];
    }

    private static SharedFileGroup Group(List<OverrideConflict> group, Func<string, string> nameOf)
    {
        var first = group[0];
        var kinds = group.GroupBy(c => Kind(c.FileName)).OrderByDescending(k => k.Count()).ThenBy(k => k.Key.One, StringComparer.Ordinal)
            .Select(k => PlanWords.Count(k.Count(), k.Key.One, k.Key.Many)).ToList();
        return new SharedFileGroup(
            $"{nameOf(first.Winner.ModId)} wins over {PlanWords.Names(first.OtherModIds, nameOf)}",
            PlanWords.List(kinds),
            [.. group.Select(c => c.FileName)],
            group.TrueForAll(c => c.SameContent))
        {
            WinnerModId = first.Winner.ModId,
            OtherModIds = first.OtherModIds,
        };
    }

    private static bool IsBehaviour(OverrideConflict conflict) => SharedFileRank.Of(conflict.FileName) == SharedFileImpact.CouldBreakQuests;

    private static (string One, string Many) Kind(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".gda" => ("data table", "data tables"),
            ".ncs" => ("script", "scripts"),
            ".tlk" => ("talk table", "talk tables"),
            ".dlg" or ".cnv" => ("conversation", "conversations"),
            ".plo" => ("plot file", "plot files"),
            ".erf" or ".rim" => ("archive", "archives"),
            ".xml" => ("settings file", "settings files"),
            _ when GameDataFiles.Looks.Contains(extension) => ("texture or model", "textures and models"),
            _ => ("other file", "other files"),
        };
    }
}
