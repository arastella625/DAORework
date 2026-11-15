using DAORework.Core.Mods.Conflicts;

namespace DAORework.Core.Mods.AddIns;

/// <summary>
/// Finds the file names two or more add-ins give the game (P2.5-23), and whose copy it uses (<see cref="AddInRanking"/>).
/// A <c>core</c> file is compared with every other add-in's; a <c>module</c> file only with add-ins that add to the same
/// campaign, since the game never loads two campaigns at once. Names are compared without case, and each archive's own
/// <c>manifest.xml</c> is left out. A name only BioWare's own add-ins share is theirs to settle, so it isn't reported.
/// Files that could break quests come first. Pure.
/// </summary>
public static class AddInClashes
{
    private static readonly StringComparer _names = StringComparer.OrdinalIgnoreCase;
    private static readonly HashSet<string> _ignored = new(_names) { "manifest.xml" };

    public static IReadOnlyList<AddInClash> Find(IReadOnlyList<AddInContents> addIns)
    {
        ArgumentNullException.ThrowIfNull(addIns);
        var core = addIns.SelectMany(a => a.CoreNames.Select(name => (Scope: string.Empty, Name: name, a.AddIn)));
        var module = addIns.SelectMany(a => a.ModuleNames.Select(name => (Scope: a.AddIn.ExtendedModule, Name: name, a.AddIn)));
        return [.. core.Concat(module)
            .Where(f => !_ignored.Contains(f.Name))
            .GroupBy(f => (Scope: f.Scope.ToUpperInvariant(), Name: f.Name.ToUpperInvariant()))
            .Select(g => (Name: g.First().Name, AddIns: g.Select(f => f.AddIn).DistinctBy(a => a.Uid, _names).ToList()))
            .Where(g => g.AddIns.Count > 1 && g.AddIns.Any(a => !a.IsOfficial))
            .Select(g => Clash(g.Name, g.AddIns))
            .DistinctBy(c => (c.FileName.ToUpperInvariant(), Uids(c)))
            .OrderBy(c => SharedFileRank.Of(c.FileName))
            .ThenBy(c => c.FileName, _names)];
    }

    private static AddInClash Clash(string fileName, List<ListedAddIn> addIns)
    {
        var winner = AddInRanking.Winner(addIns);
        return new AddInClash(fileName, winner, [.. AddInRanking.LoadOrder(addIns.Where(a => a != winner))]);
    }

    private static string Uids(AddInClash clash) =>
        string.Join('|', clash.Others.Select(o => o.Uid.ToUpperInvariant()).Order().Prepend(clash.Winner.Uid.ToUpperInvariant()));
}
