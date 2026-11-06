namespace DAORework.Core.Mods.Library;

/// <summary>
/// Finds the mods the library holds more than once (P2.6-18): add-ins with one UID, which cannot be on together, and
/// other mods with the same mod files. The game finds override files by name in any subfolder (ADR-0012), so a folder
/// is no difference: a mod managed once from its own folder and once from inside it is one mod, under either name.
/// Different versions of an override mod are different mods. Pure.
/// </summary>
public static class ModCopies
{
    /// <summary>Each mod that has another copy, by its id; a mod with none is not listed.</summary>
    public static IReadOnlyDictionary<string, ModCopy> Find(IReadOnlyList<LibraryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var copies = new Dictionary<string, ModCopy>(StringComparer.Ordinal);
        var groups = entries
            .Select(e => (Entry: e, Key: KeyOf(e.Mod)))
            .Where(e => e.Key is not null)
            .GroupBy(e => e.Key!, e => e.Entry, StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups.Where(g => g.Count() > 1))
        {
            var newestFirst = group.OrderByDescending(e => e.ImportedAtUtc).ThenBy(e => e.Mod.Id, StringComparer.Ordinal).ToList();
            for (var rank = 0; rank < newestFirst.Count; rank++)
            {
                var mod = newestFirst[rank].Mod;
                copies[mod.Id] = new ModCopy(group.Key, rank, newestFirst.Count)
                {
                    AlsoNamed = newestFirst.Select(e => e.Mod.Name).FirstOrDefault(n => !StringComparer.OrdinalIgnoreCase.Equals(n, mod.Name)),
                };
            }
        }

        return copies;
    }

    /// <summary>What makes two mods copies, or null for a mod with nothing for the game.</summary>
    private static string? KeyOf(LibraryMod mod)
    {
        if (mod.AddInUid is not null)
        {
            return "add-in:" + mod.AddInUid;
        }

        var files = mod.Files
            .Where(f => !NonModFiles.Is(f.RelativePath))
            .Select(f => $"{f.RelativePath[(f.RelativePath.LastIndexOf('/') + 1)..]}|{f.Sha256}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return files.Count == 0 ? null : "files:" + string.Join('\n', files);
    }
}
