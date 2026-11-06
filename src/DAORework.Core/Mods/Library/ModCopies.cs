namespace DAORework.Core.Mods.Library;

/// <summary>
/// Finds the mods the library holds more than once (P2.6-18): add-ins with one UID, which cannot be on together, other
/// mods with the same mod files, and mods that are only part of another (<see cref="ModParts"/>). The game finds override
/// files by name in any subfolder (ADR-0012), so a folder is no difference: a mod managed once from its own folder and
/// once from inside it is one mod, under either name. Different versions of an override mod are different mods. An entry
/// with nothing for the game, only a readme or a Mac leftover, is part of the mod of its name (P2.6-52). Pure.
/// </summary>
public static class ModCopies
{
    /// <summary>Each mod that has another copy, by its id; a mod with none is not listed.</summary>
    /// <param name="entries">The library.</param>
    /// <param name="on">The mods on in the selected folder, whose contents a smaller mod can't add to.</param>
    public static IReadOnlyDictionary<string, ModCopy> Find(IReadOnlyList<LibraryEntry> entries, IEnumerable<string>? on = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var keyed = entries.Select(e => (Entry: e, Key: KeyOf(e.Mod))).ToList();
        var identical = keyed
            .Where(e => e.Key is not null)
            .GroupBy(e => e.Key!, e => e.Entry, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var wholes = ModParts.Wholes(identical, on ?? []);
        var copies = new Dictionary<string, ModCopy>(StringComparer.Ordinal);
        var leftovers = keyed.Where(e => e.Key is null).Select(e => e.Entry).ToList();
        foreach (var set in identical.GroupBy(g => wholes.GetValueOrDefault(g.Key, g.Key), StringComparer.OrdinalIgnoreCase))
        {
            List<LibraryEntry> members = [.. set.SelectMany(g => g)];
            Add(copies, set.Key, [.. members, .. Claim(leftovers, members)]);
        }

        return copies;
    }

    /// <summary>Takes the leftovers named as one of these mods out of the list: a leftover belongs to one set, or the sets would disagree.</summary>
    private static List<LibraryEntry> Claim(List<LibraryEntry> leftovers, List<LibraryEntry> members)
    {
        var names = members.Select(e => ModParts.Plain(e.Mod.Name)).Where(n => n.Length > 0).ToHashSet(StringComparer.Ordinal);
        var claimed = leftovers.Where(e => names.Contains(ModParts.Plain(e.Mod.Name))).ToList();
        leftovers.RemoveAll(claimed.Contains);
        return claimed;
    }

    /// <summary>The names and hashes of a mod's game files, which is how the game tells them apart.</summary>
    internal static List<string> GameFiles(LibraryMod mod) => mod.Files
        .Where(f => !NonModFiles.Is(f.RelativePath))
        .Select(f => $"{f.RelativePath[(f.RelativePath.LastIndexOf('/') + 1)..]}|{f.Sha256}")
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static void Add(Dictionary<string, ModCopy> copies, string group, List<LibraryEntry> set)
    {
        if (set.Count < 2)
        {
            return;
        }

        var everything = set.SelectMany(e => Hashes(e.Mod)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var holders = set.Where(e => Hashes(e.Mod).Count == everything.Count).ToHashSet();
        var ordered = set.OrderByDescending(holders.Contains).ThenByDescending(e => e.ImportedAtUtc).ThenBy(e => e.Mod.Id, StringComparer.Ordinal).ToList();
        for (var rank = 0; rank < ordered.Count; rank++)
        {
            var entry = ordered[rank];
            copies[entry.Mod.Id] = new ModCopy(group, rank, ordered.Count)
            {
                AlsoNamed = ordered.Select(e => e.Mod.Name).FirstOrDefault(n => !StringComparer.OrdinalIgnoreCase.Equals(n, entry.Mod.Name)),
                HoldsAll = holders.Contains(entry),
                IsPart = holders.Count > 0 && !holders.Contains(entry),
            };
        }
    }

    private static HashSet<string> Hashes(LibraryMod mod) =>
        mod.Files.Where(f => !NonModFiles.Is(f.RelativePath)).Select(f => f.Sha256).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>What makes two mods copies, or null for a mod with nothing for the game.</summary>
    private static string? KeyOf(LibraryMod mod)
    {
        if (mod.AddInUid is not null)
        {
            return "add-in:" + mod.AddInUid;
        }

        var files = GameFiles(mod);
        return files.Count == 0 ? null : "files:" + string.Join('\n', files);
    }
}
