using DAORework.Core.Catalog;

namespace DAORework.Core.Mods.Adoption;

/// <summary>The known-items catalog as the adoption scan uses it: which add-in UIDs are official DLC, and which entry a candidate matches.</summary>
internal sealed class AdoptionCatalog
{
    private readonly HashSet<string> _officialUids;
    private readonly List<KnownItem> _items;

    public AdoptionCatalog(KnownItemsCatalog catalog)
    {
        _officialUids = catalog.Items
            .Where(i => i.Kind == KnownItemKind.Official)
            .SelectMany(i => i.Fingerprints)
            .Where(f => f.Kind == FingerprintKind.AddInUid)
            .Select(f => f.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _items = catalog.Items.Where(i => i.Kind != KnownItemKind.Official).ToList();
    }

    public bool IsOfficial(string uid) => _officialUids.Contains(uid);

    public KnownItem? ForAddIn(string uid) => First(FingerprintKind.AddInUid, value => value.Equals(uid, StringComparison.OrdinalIgnoreCase));

    public KnownItem? ForOverrideFiles(IReadOnlyList<HandInstalledFile> files)
    {
        var names = files.Select(f => f.Path[(f.Path.LastIndexOf('/') + 1)..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return First(FingerprintKind.OverrideFile, names.Contains);
    }

    private KnownItem? First(FingerprintKind kind, Func<string, bool> matches) =>
        _items.FirstOrDefault(item => item.Fingerprints.Any(f => f.Kind == kind && matches(f.Value)));
}
