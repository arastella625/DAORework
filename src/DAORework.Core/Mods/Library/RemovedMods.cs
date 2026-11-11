using DAORework.Core.Fixes;

namespace DAORework.Core.Mods.Library;

/// <summary>
/// The mods an earlier state names that have left the Library (ADR-0039). Going back to it would leave a record no later
/// plan could use. A fix's entry lives elsewhere, so it never counts. Pure.
/// </summary>
public static class RemovedMods
{
    /// <returns>Their names as a person reads them, since the mods themselves are gone.</returns>
    public static IReadOnlyList<string> In(IReadOnlyList<string> activeModIds, IEnumerable<string> libraryIds) =>
        [.. Ids(activeModIds, libraryIds).Select(ModIds.ReadableName)];

    /// <returns>Their ids, which are also the names of their folder and <c>.mod.json</c> in the Recycle Bin.</returns>
    public static IReadOnlyList<string> Ids(IReadOnlyList<string> activeModIds, IEnumerable<string> libraryIds)
    {
        ArgumentNullException.ThrowIfNull(activeModIds);
        ArgumentNullException.ThrowIfNull(libraryIds);
        var library = libraryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. activeModIds.Where(id => !FixIds.IsFixEntry(id) && !library.Contains(id))];
    }
}
