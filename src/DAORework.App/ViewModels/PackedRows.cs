using DAORework.Core.Mods.Conflicts;

namespace DAORework.App.ViewModels;

/// <summary>
/// What <i>Combined files</i> lists for files inside archives (P2.6-65). One real pair of mods shared 41 files that could break
/// quests, which read as 41 rows saying the same thing, so many files with the same mods and the same risk are one row.
/// </summary>
public static class PackedRows
{
    private const int MaxRows = 50;

    private const int MostFilesShownAlone = 3;

    /// <param name="packed">What the mods share inside archives.</param>
    /// <param name="names">Mod names by id.</param>
    public static IReadOnlyList<FileConflictRow> For(IReadOnlyList<PackedConflict> packed, IReadOnlyDictionary<string, string> names)
    {
        ArgumentNullException.ThrowIfNull(packed);
        ArgumentNullException.ThrowIfNull(names);
        string Mods(PackedConflict conflict) => string.Join(", ", conflict.ModIds.Select(id => names.TryGetValue(id, out var name) ? name : id));
        var rows = packed
            .GroupBy(p => (Mods: Mods(p), Impact: SharedFileRank.Of(p.FileName)))
            .SelectMany(set => set.Count() > MostFilesShownAlone
                ? [FileConflictRow.PackedSet([.. set], set.Key.Mods, set.Key.Impact)]
                : set.Select(p => FileConflictRow.Packed(p, set.Key.Mods)))
            .OrderBy(r => r.Impact)
            .ToList();
        return rows.Count <= MaxRows
            ? rows
            : [.. rows.Take(MaxRows), new FileConflictRow($"{rows.Count - MaxRows} more", "Inside an archive", "More files these mods share inside their archives.")];
    }
}
