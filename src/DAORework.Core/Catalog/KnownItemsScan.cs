using DAORework.Core.Common;

namespace DAORework.Core.Catalog;

/// <summary>
/// What a scan of one profile found against the catalog. An item whose fingerprints could not all be checked is in
/// <see cref="Unresolved"/> rather than <see cref="Missing"/>, so an unreadable file is never reported as "not installed".
/// </summary>
public sealed record KnownItemsScan(
    IReadOnlyList<DetectedKnownItem> Found,
    IReadOnlyList<KnownItem> Missing,
    IReadOnlyList<Error> Unresolved,
    IReadOnlyList<KnownConflict> Conflicts)
{
    /// <summary>Sorts the catalog's items into found, missing and unresolved, and keeps the conflicts whose two sides were both found.</summary>
    public static KnownItemsScan Create(
        KnownItemsCatalog catalog,
        IReadOnlyList<DetectedKnownItem> found,
        IReadOnlyDictionary<string, Error> unresolved)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(found);
        ArgumentNullException.ThrowIfNull(unresolved);

        var foundIds = found.Select(f => f.Item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = catalog.Items
            .Where(item => !foundIds.Contains(item.Id) && !unresolved.ContainsKey(item.Id))
            .ToList();
        var conflicts = catalog.Conflicts
            .Where(c => foundIds.Contains(c.First) && foundIds.Contains(c.Second))
            .ToList();

        return new KnownItemsScan(found, missing, unresolved.Values.ToList(), conflicts);
    }
}
