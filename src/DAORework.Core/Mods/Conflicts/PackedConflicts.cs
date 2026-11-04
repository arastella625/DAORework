namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// Finds a resource that active mods share however it is packed (ROADMAP P3-07): inside two mods' <c>.erf</c> archives, or
/// inside one and as a loose file in another. Two loose files are the scanner's job, and a mod never conflicts with
/// itself. Pure: the caller reads the archives.
/// </summary>
public static class PackedConflicts
{
    /// <summary>Add-ins each carry one in their archives to describe themselves, so sharing the name is no clash.</summary>
    private const string AddInManifest = "manifest.xml";

    /// <param name="archives">The archives in the active mods.</param>
    /// <param name="looseFiles">Each active mod's loose override files, by file name.</param>
    public static IReadOnlyList<PackedConflict> Find(IReadOnlyList<PackedArchive> archives, IReadOnlyList<(string ModId, string FileName)> looseFiles)
    {
        ArgumentNullException.ThrowIfNull(archives);
        ArgumentNullException.ThrowIfNull(looseFiles);
        var names = StringComparer.OrdinalIgnoreCase;
        var packed = archives.SelectMany(a => a.Resources.Where(r => !names.Equals(r, AddInManifest)).Select(r => (a.ModId, FileName: r))).ToList();
        var inArchive = packed.Select(p => p.FileName).ToHashSet(names);
        return [.. packed.Concat(looseFiles.Where(l => inArchive.Contains(l.FileName)))
            .GroupBy(p => p.FileName, names)
            .Select(g => new PackedConflict(g.Key, [.. g.Select(p => p.ModId).Distinct(names)]))
            .Where(c => c.ModIds.Count > 1)
            .OrderBy(c => c.FileName, names)];
    }
}
