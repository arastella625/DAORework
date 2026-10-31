using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;

namespace DAORework.Application.Mods.Adoption;

/// <summary>
/// Lists the hand-installed mods in a documents folder that DAORework could adopt, and what it cannot adopt and why
/// (ROADMAP P2.5-02, ADR-0015), with the library mod each candidate is or is closest to. Only reads; only files a
/// library mod could match by size are hashed. Without the catalog the official DLC cannot be told apart, so a
/// catalog that does not load fails the scan rather than offering it.
/// </summary>
public sealed class ScanForAdoption
{
    private readonly IHandInstalledModsReader _reader;
    private readonly IDeploymentRecordStore _records;
    private readonly IKnownItemsCatalogSource _catalog;
    private readonly IModLibrary _library;
    private readonly IDocumentsScanner _hasher;

    public ScanForAdoption(IHandInstalledModsReader reader, IDeploymentRecordStore records, IKnownItemsCatalogSource catalog, IModLibrary library, IDocumentsScanner hasher)
    {
        _reader = reader;
        _records = records;
        _catalog = catalog;
        _library = library;
        _hasher = hasher;
    }

    public async Task<Result<AdoptionScan>> ExecuteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        if (!_catalog.Load().TryGetValue(out var catalog, out var error)
            || !(await _records.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var record, out error)
            || !(await _reader.ReadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var snapshot, out error))
        {
            return Result.Failure<AdoptionScan>(error);
        }

        var scan = AdoptionScanner.Scan(snapshot, record, catalog);
        var library = (await _library.ListAsync(cancellationToken)).Select(e => e.Mod).ToList();
        var paths = AdoptionLibraryMatcher.PathsToHash(snapshot, scan, library);
        if (paths.Count == 0)
        {
            return Result.Success(scan);
        }

        return (await _hasher.ScanAsync(documentsDirectory, paths, cancellationToken)).TryGetValue(out var hashed, out error)
            ? Result.Success(AdoptionLibraryMatcher.Match(snapshot, scan, library, hashed.Hashes))
            : Result.Failure<AdoptionScan>(error);
    }
}
