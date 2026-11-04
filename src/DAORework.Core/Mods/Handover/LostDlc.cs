using DAORework.Core.Catalog;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Handover;

/// <summary>
/// The official DLC a purge took out of <c>AddIns.xml</c> (ADR-0028 §3): entries the kept list has, the list no longer
/// has, and the catalog calls official. Only official entries: anything else was the player's or Vortex's to remove. Pure.
/// </summary>
public static class LostDlc
{
    public static IReadOnlyList<AddInsListChange> Find(IReadOnlyList<KeptAddIn> kept, IReadOnlyList<RegisteredAddIn> registered, KnownItemsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(kept);
        ArgumentNullException.ThrowIfNull(registered);
        ArgumentNullException.ThrowIfNull(catalog);
        var known = new AdoptionCatalog(catalog);
        var there = registered.Select(a => a.Uid).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. kept.Where(k => known.IsOfficial(k.Uid) && !there.Contains(k.Uid))
            .Select(k => new AddInsListChange(k.Uid, NameOf(catalog, k.Uid), Add: true) { Item = k.Item })];
    }

    private static string NameOf(KnownItemsCatalog catalog, string uid) =>
        catalog.Items.FirstOrDefault(i => i.Fingerprints.Any(f => f.Kind == FingerprintKind.AddInUid && f.Value.Equals(uid, StringComparison.OrdinalIgnoreCase)))?.Name ?? uid;
}
