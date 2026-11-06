namespace DAORework.Core.Mods.Library;

/// <summary>
/// What keeping one copy of every mod held more than once means (ADR-0039). Each set is planned by
/// <see cref="KeepOneCopy"/>. A set where one copy holds everything starts on that copy, and one where none does stays
/// as it is until the player picks, so nothing is lost by not reading. A set that can't be cleared stays too, the others
/// go ahead, and their swaps become one order. Pure.
/// </summary>
public static class KeepOneOfEach
{
    private static readonly StringComparer _ids = StringComparer.OrdinalIgnoreCase;

    /// <param name="entries">The Library.</param>
    /// <param name="picks">The player's picks by group: the copy to keep, or null to keep them all. A set with none starts as the summary says.</param>
    /// <param name="selectedDocuments">The documents folder a swap can be made in, if any.</param>
    /// <param name="folders">Every documents folder DAORework knows.</param>
    public static KeepEachPlan Plan(IReadOnlyList<LibraryEntry> entries, IReadOnlyDictionary<string, string?> picks, string? selectedDocuments, IReadOnlyList<FolderMods> folders)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(picks);
        ArgumentNullException.ThrowIfNull(folders);

        // An unfinished change is said once for the whole plan, not once for every set.
        var settled = folders.Select(f => f with { Unfinished = false }).ToList();
        var selected = settled.FirstOrDefault(f => selectedDocuments is not null && _ids.Equals(f.DocumentsDirectory, selectedDocuments));
        var copies = ModCopies.Find(entries, selected?.ActiveModIds);
        var sets = entries
            .Where(e => copies.ContainsKey(e.Mod.Id))
            .GroupBy(e => copies[e.Mod.Id].Group, StringComparer.OrdinalIgnoreCase)
            .Select(g => Set(g.Key, [.. g.OrderBy(e => copies[e.Mod.Id].Rank)], copies, picks, selected, settled))
            .OrderByDescending(s => s.Differs)
            .ThenBy(s => s.Copies[0].Mod.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new KeepEachPlan(sets)
        {
            Problems = KeepOneCopy.Unfinished(folders),
            SwappedOrder = selected is null ? null : Swapped(selected.ActiveModIds, sets),
        };
    }

    private static KeepEachSet Set(string group, List<LibraryEntry> copies, IReadOnlyDictionary<string, ModCopy> found, IReadOnlyDictionary<string, string?> picks, FolderMods? selected, List<FolderMods> folders)
    {
        var holders = copies.Where(e => found[e.Mod.Id].HoldsAll).ToList();
        var picked = picks.TryGetValue(group, out var id);
        var chosen = copies.FirstOrDefault(e => picked && _ids.Equals(e.Mod.Id, id));
        var kept = chosen ?? StartingPick(holders.Count > 0 ? holders : copies, selected, folders);
        var plan = KeepOneCopy.Plan(kept, [.. copies.Where(e => !ReferenceEquals(e, kept))], selected?.DocumentsDirectory, folders);
        return new KeepEachSet(group, copies, plan)
        {
            On = [.. copies.Where(e => IsOn(e, selected)).Select(e => e.Mod.Id)],
            Differs = holders.Count == 0,
            HasParts = copies.Any(e => found[e.Mod.Id].IsPart),
            KeepsAll = (picked && id is null) || (chosen is null && holders.Count == 0),
        };
    }

    /// <summary>The copy on here, or else one on somewhere else, which would otherwise stop the set, or else the newest.</summary>
    private static LibraryEntry StartingPick(List<LibraryEntry> newestFirst, FolderMods? selected, List<FolderMods> folders)
    {
        return newestFirst.FirstOrDefault(e => IsOn(e, selected))
            ?? newestFirst.FirstOrDefault(e => folders.Any(f => IsOn(e, f)))
            ?? newestFirst[0];
    }

    private static bool IsOn(LibraryEntry entry, FolderMods? folder) => folder?.ActiveModIds.Contains(entry.Mod.Id, _ids) == true;

    /// <summary>Each swap made on the order the last one left, or null when no set being cleared has one.</summary>
    private static List<string>? Swapped(IReadOnlyList<string> active, List<KeepEachSet> sets)
    {
        List<string>? order = null;
        foreach (var plan in sets.Where(s => s.Clears).Select(s => s.Plan))
        {
            var removing = plan.Removing.Select(e => e.Mod.Id).ToHashSet(_ids);
            order = KeepOneCopy.Swap(order ?? active, plan.Kept.Mod.Id, removing) ?? order;
        }

        return order;
    }
}
