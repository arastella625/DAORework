using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Library;

/// <summary>
/// What keeping one copy of a mod means (ADR-0039): the other copies leave the Library, a copy on in the selected folder
/// gives its place to the kept one, and a copy on anywhere else stops it, as does a change that didn't finish. Pure.
/// </summary>
public static class KeepOneCopy
{
    private static readonly StringComparer _ids = StringComparer.OrdinalIgnoreCase;

    /// <param name="kept">The copy to keep.</param>
    /// <param name="others">The copies to remove.</param>
    /// <param name="selectedDocuments">The documents folder a swap can be made in, if any.</param>
    /// <param name="folders">Every documents folder DAORework knows.</param>
    public static KeepCopyPlan Plan(LibraryEntry kept, IReadOnlyList<LibraryEntry> others, string? selectedDocuments, IReadOnlyList<FolderMods> folders)
    {
        ArgumentNullException.ThrowIfNull(kept);
        ArgumentNullException.ThrowIfNull(others);
        ArgumentNullException.ThrowIfNull(folders);

        var removing = others.Select(e => e.Mod.Id).ToHashSet(_ids);
        var selected = folders.FirstOrDefault(f => selectedDocuments is not null && StringComparer.OrdinalIgnoreCase.Equals(f.DocumentsDirectory, selectedDocuments));
        return new KeepCopyPlan(kept, others)
        {
            OnlyInRemoved = OnlyIn(others, kept),
            Problems = Problems(others, selected, folders),
            SwappedOrder = selected is null ? null : Swap(selected.ActiveModIds, kept.Mod.Id, removing),
            SwappedOut = others.FirstOrDefault(e => selected?.ActiveModIds.Contains(e.Mod.Id, _ids) == true)?.Mod.Name,
        };
    }

    /// <summary>The order with the kept copy where the first removed one was, or null when no removed copy is on.</summary>
    private static List<string>? Swap(IReadOnlyList<string> active, string kept, HashSet<string> removing)
    {
        if (!active.Any(removing.Contains))
        {
            return null;
        }

        var alreadyOn = active.Contains(kept, _ids);
        var order = new List<string>();
        foreach (var id in active)
        {
            if (!removing.Contains(id))
            {
                order.Add(id);
            }
            else if (!alreadyOn)
            {
                order.Add(kept);
                alreadyOn = true;
            }
        }

        return order;
    }

    private static List<PlanProblem> Problems(IReadOnlyList<LibraryEntry> others, FolderMods? selected, IReadOnlyList<FolderMods> folders)
    {
        var problems = folders
            .Where(f => f.Unfinished)
            .Select(f => new PlanProblem($"A change in {f.DocumentsDirectory} didn't finish.", "Finish it or undo it in History first."))
            .ToList();
        foreach (var folder in folders.Where(f => !ReferenceEquals(f, selected)))
        {
            problems.AddRange(others
                .Where(e => folder.ActiveModIds.Contains(e.Mod.Id, _ids))
                .Select(e => new PlanProblem($"{ModDisplayName.For(e.Mod.Name)} is on in {folder.DocumentsDirectory}.", "Turn it off there first.")));
        }

        return problems;
    }

    /// <summary>The game files only the removed copies hold, by name: what goes for good.</summary>
    private static List<string> OnlyIn(IReadOnlyList<LibraryEntry> others, LibraryEntry kept)
    {
        var keptHashes = kept.Mod.Files.Select(f => f.Sha256).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return others
            .SelectMany(e => e.Mod.Files)
            .Where(f => !NonModFiles.Is(f.RelativePath) && !keptHashes.Contains(f.Sha256))
            .Select(f => f.RelativePath[(f.RelativePath.LastIndexOf('/') + 1)..])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
