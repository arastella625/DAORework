using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;

namespace DAORework.Application.Mods.Library;

/// <summary>Says whether a library mod is a download the catalog knows, or differs from one (ADR-0018). Only reads.</summary>
public sealed class CheckKnownDownload
{
    private readonly IKnownItemsCatalogSource _catalogSource;

    public CheckKnownDownload(IKnownItemsCatalogSource catalogSource)
    {
        _catalogSource = catalogSource;
    }

    public Result<DownloadCheck> Execute(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return _catalogSource.Load().TryGetValue(out var catalog, out var error)
            ? Result.Success(KnownDownloads.Check(catalog, entry))
            : Result.Failure<DownloadCheck>(error);
    }
}
