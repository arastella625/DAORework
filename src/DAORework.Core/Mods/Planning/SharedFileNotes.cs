using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// Files several mods ship, as a few sentences however many there are: how many, which of them lose a mod's behaviour
/// rather than a look, then one line for each set of mods, biggest first. Pure.
/// </summary>
internal static class SharedFileNotes
{
    private const int GroupsShown = 4;

    private static readonly HashSet<string> _behaviour = new(StringComparer.OrdinalIgnoreCase) { ".gda", ".ncs", ".tlk" };

    /// <param name="conflicts">The conflicts between mods whose copies differ.</param>
    /// <param name="nameOf">How a mod is named.</param>
    public static IReadOnlyList<string> For(IReadOnlyList<OverrideConflict> conflicts, Func<string, string> nameOf)
    {
        if (conflicts.Count == 0)
        {
            return [];
        }

        var notes = new List<string> { $"{ModActivationPlanWarnings.Files(conflicts.Count)} are in more than one mod. For each one, the mod lower in the order wins." };
        var behaviour = conflicts.Count(IsBehaviour);
        if (behaviour > 0)
        {
            notes.Add($"{PlanWords.Count(behaviour, "of them is a data table, script or talk table", "of them are data tables, scripts or talk tables")}, where a mod's changes are lost. Look for a compatibility patch.");
        }

        var groups = conflicts
            .GroupBy(c => $"{c.Winner.ModId}\n{string.Join('\n', c.OtherModIds.Order(StringComparer.OrdinalIgnoreCase))}", StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Any(IsBehaviour))
            .ThenByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        notes.AddRange(groups.Take(GroupsShown).Select(g => Line([.. g], nameOf)));
        if (groups.Count > GroupsShown)
        {
            notes.Add($"{PlanWords.Count(groups.Count - GroupsShown, "more set of mods shares", "more sets of mods share")} files. Show details lists them.");
        }

        return notes;
    }

    private static string Line(List<OverrideConflict> group, Func<string, string> nameOf)
    {
        var first = group[0];
        var kinds = group.GroupBy(c => Kind(c.FileName)).OrderByDescending(k => k.Count()).ThenBy(k => k.Key.One, StringComparer.Ordinal)
            .Select(k => PlanWords.Count(k.Count(), k.Key.One, k.Key.Many)).ToList();
        return $"{nameOf(first.Winner.ModId)} wins over {PlanWords.Names(first.OtherModIds, nameOf)}: {PlanWords.List(kinds)}.";
    }

    private static bool IsBehaviour(OverrideConflict conflict) => _behaviour.Contains(Path.GetExtension(conflict.FileName));

    private static (string One, string Many) Kind(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".gda" => ("data table", "data tables"),
            ".ncs" => ("script", "scripts"),
            ".tlk" => ("talk table", "talk tables"),
            ".erf" or ".rim" => ("archive", "archives"),
            ".xml" => ("settings file", "settings files"),
            _ when GameDataFiles.Looks.Contains(extension) => ("texture or model", "textures and models"),
            _ => ("other file", "other files"),
        };
    }
}
