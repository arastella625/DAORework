using DAORework.Core.Catalog;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Safety;

/// <summary>
/// Compares a library mod's download with the catalog's <see cref="FingerprintKind.DownloadSha256"/> fingerprints
/// (ADR-0018). A download that differs from every known one is only a warning: authors update their files.
/// </summary>
public static class KnownDownloads
{
    private static readonly string[] _archiveExtensions = [".zip", ".dazip", ".7z", ".rar"];

    public static DownloadCheck Check(KnownItemsCatalog catalog, LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Origin.Kind != ModOriginKind.Download)
        {
            return DownloadCheck.Unknown;
        }

        var match = catalog.Items
            .SelectMany(item => Downloads(item).Select(download => (item, download)))
            .FirstOrDefault(m => m.download.Value.Equals(entry.SourceSha256, StringComparison.OrdinalIgnoreCase));
        if (match.item is not null)
        {
            return new DownloadCheck(DownloadCheckKind.Matches, match.item, match.download.Version);
        }

        var recognized = catalog.Items.FirstOrDefault(item => Downloads(item).Any() && Recognizes(item, entry.Mod));
        if (recognized is null)
        {
            return DownloadCheck.Unknown;
        }

        return new DownloadCheck(IsArchive(entry.SourceName) ? DownloadCheckKind.Differs : DownloadCheckKind.FromAFolder, recognized);
    }

    private static IEnumerable<KnownItemFingerprint> Downloads(KnownItem item) =>
        item.Fingerprints.Where(f => f.Kind == FingerprintKind.DownloadSha256);

    private static bool Recognizes(KnownItem item, LibraryMod mod)
    {
        var names = mod.Files.Select(f => f.RelativePath[(f.RelativePath.LastIndexOf('/') + 1)..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return item.Fingerprints.Any(f => f.Kind switch
        {
            FingerprintKind.AddInUid => f.Value.Equals(mod.AddInUid, StringComparison.OrdinalIgnoreCase),
            FingerprintKind.OverrideFile => names.Contains(f.Value),
            _ => false,
        });
    }

    private static bool IsArchive(string sourceName) =>
        _archiveExtensions.Any(extension => sourceName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
}
