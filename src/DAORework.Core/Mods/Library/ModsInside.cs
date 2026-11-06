namespace DAORework.Core.Mods.Library;

/// <summary>
/// Finds the override mods whose files are all in a bigger one, whatever they're called and whatever is on (ADR-0041).
/// Unlike <see cref="ModParts"/> this only informs: the player may want the smaller mod, so it isn't a copy. Pure.
/// </summary>
public static class ModsInside
{
    private const string Archive = ".erf";

    /// <param name="entries">The library.</param>
    /// <param name="archiveNames">The resources in a mod's archive, or null when it can't be read.</param>
    /// <returns>The biggest mod each mod is inside, by the smaller mod's id. A mod inside nothing isn't listed.</returns>
    public static IReadOnlyDictionary<string, ModInside> Find(IReadOnlyList<LibraryEntry> entries, Func<LibraryMod, LibraryModFile, IReadOnlyList<string>?> archiveNames)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(archiveNames);
        var mods = entries.Where(e => e.Mod.Kind == ModKind.Override && e.Mod.AddInUid is null).Select(e => Contents.Of(e.Mod, archiveNames)).Where(c => c.Exact.Count > 0).ToList();
        var found = new Dictionary<string, ModInside>(StringComparer.Ordinal);
        foreach (var part in mods)
        {
            var whole = mods.Where(m => m.IsBiggerThan(part) && m.Holds(part)).OrderByDescending(m => m.Resources.Count).ThenBy(m => m.Mod.Id, StringComparer.Ordinal).FirstOrDefault();
            if (whole is not null)
            {
                found[part.Mod.Id] = new ModInside(whole.Mod.Id, whole.Mod.Name, !whole.HoldsExactly(part));
            }
        }

        return found;
    }

    /// <param name="Mod">The mod.</param>
    /// <param name="Exact">Each game file, by name and hash.</param>
    /// <param name="Resources">The name of everything it gives the game, loose or inside an archive.</param>
    /// <param name="Files">Each game file with what it holds: nothing for a loose file or an archive that can't be read.</param>
    private sealed record Contents(LibraryMod Mod, HashSet<string> Exact, HashSet<string> Resources, List<(string Key, IReadOnlyList<string> Inner)> Files)
    {
        public static Contents Of(LibraryMod mod, Func<LibraryMod, LibraryModFile, IReadOnlyList<string>?> archiveNames)
        {
            var files = new List<(string, IReadOnlyList<string>)>();
            var resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in mod.Files.Where(f => !NonModFiles.Is(f.RelativePath)))
            {
                var name = file.RelativePath[(file.RelativePath.LastIndexOf('/') + 1)..];
                var inner = name.EndsWith(Archive, StringComparison.OrdinalIgnoreCase) ? archiveNames(mod, file) ?? [] : [];
                files.Add(($"{name}|{file.Sha256}", inner));
                resources.UnionWith(inner.Count > 0 ? inner : [name]);
            }

            return new Contents(mod, [.. files.Select(f => f.Item1)], resources, files);
        }

        public bool IsBiggerThan(Contents other) => Resources.Count > other.Resources.Count || Exact.Count > other.Exact.Count;

        public bool Holds(Contents part) => part.Files.TrueForAll(f => Exact.Contains(f.Key) || (f.Inner.Count > 0 && Resources.IsSupersetOf(f.Inner)));

        public bool HoldsExactly(Contents part) => part.Exact.IsSubsetOf(Exact);
    }
}
