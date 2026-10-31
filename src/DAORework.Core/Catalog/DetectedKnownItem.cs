namespace DAORework.Core.Catalog;

/// <summary>A catalog item found on disk, with every place one of its fingerprints matched.</summary>
public sealed record DetectedKnownItem(KnownItem Item, IReadOnlyList<FingerprintMatch> Matches)
{
    /// <summary>Gets the first version any match reported, or null when none of the fingerprints carries one.</summary>
    public string? Version => Matches.Select(m => m.Version).FirstOrDefault(v => v is not null);

    /// <summary>Gets the item's name with its version when one was read, for example "DXVK v2.3".</summary>
    public string DisplayName => Version is null ? Item.Name : $"{Item.Name} {Version}";
}
