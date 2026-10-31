using DAORework.Core.Catalog;

namespace DAORework.Core.Mods.Safety;

/// <summary>
/// A library mod's download compared with the catalog's known downloads (ADR-0018). <see cref="Item"/> is the catalog
/// entry it was compared with, and <see cref="Version"/> the version of the download it matches.
/// </summary>
public sealed record DownloadCheck(DownloadCheckKind Kind, KnownItem? Item = null, string? Version = null)
{
    public static DownloadCheck Unknown { get; } = new(DownloadCheckKind.Unknown);
}
