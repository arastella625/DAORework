namespace DAORework.Core.Mods.Library;

/// <summary>
/// Finds the override mods that are only part of another (ADR-0039): every game file, by name and hash, is in the bigger
/// one, and either the two have one name once punctuation and case are ignored, or the bigger one is on, so the smaller
/// adds nothing to the game. The same name with different contents is not a part. Pure.
/// </summary>
internal static class ModParts
{
    /// <param name="identical">The library's mods, each group holding the same game files under one key.</param>
    /// <param name="on">The mods that are on.</param>
    /// <returns>The key of the whole each part belongs to, by the part's key. A mod that is no part isn't listed.</returns>
    public static Dictionary<string, string> Wholes(IEnumerable<IGrouping<string, LibraryEntry>> identical, IEnumerable<string> on)
    {
        var active = on.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mods = identical.Where(g => g.All(e => e.Mod.Kind == ModKind.Override && e.Mod.AddInUid is null)).Select(g => Candidate.From(g, active)).ToList();
        var parents = mods.Select(m => (m.Key, Whole: WholeOf(m, mods))).Where(m => m.Whole is not null).ToDictionary(m => m.Key, m => m.Whole!.Key, StringComparer.OrdinalIgnoreCase);
        return parents.Keys.ToDictionary(k => k, k => Top(k, parents), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The bigger mod this is part of: one of its own name first, then the biggest. A strict superset, so a chain always ends.</summary>
    private static Candidate? WholeOf(Candidate part, List<Candidate> mods) => mods
        .Where(m => m.Files.Count > part.Files.Count && (m.IsOn || m.Names.Overlaps(part.Names)) && part.Files.IsSubsetOf(m.Files))
        .OrderByDescending(m => m.Names.Overlaps(part.Names))
        .ThenByDescending(m => m.Files.Count)
        .ThenBy(m => m.Key, StringComparer.Ordinal)
        .FirstOrDefault();

    private static string Top(string key, Dictionary<string, string> parents)
    {
        while (parents.TryGetValue(key, out var parent))
        {
            key = parent;
        }

        return key;
    }

    /// <summary>A mod's name without punctuation or letter case, so two spellings of one name match.</summary>
    internal static string Plain(string name) =>
        string.Concat(ModDisplayName.For(name).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant));

    private sealed record Candidate(string Key, HashSet<string> Files, HashSet<string> Names, bool IsOn)
    {
        public static Candidate From(IGrouping<string, LibraryEntry> mods, HashSet<string> active) => new(
            mods.Key,
            ModCopies.GameFiles(mods.First().Mod).ToHashSet(StringComparer.OrdinalIgnoreCase),
            mods.Select(e => Plain(e.Mod.Name)).Where(n => n.Length > 0).ToHashSet(StringComparer.Ordinal),
            mods.Any(e => active.Contains(e.Mod.Id)));
    }
}
