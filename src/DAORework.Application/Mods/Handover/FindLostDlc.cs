using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Handover;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Handover;

/// <summary>Finds the official DLC a purge took out of <c>AddIns.xml</c> (ADR-0028 §3), as changes that put it back. Only reads.</summary>
public sealed class FindLostDlc
{
    private readonly IHandInstalledModsReader _reader;
    private readonly IVortexHandoverStore _store;
    private readonly IKnownItemsCatalogSource _catalog;

    public FindLostDlc(IHandInstalledModsReader reader, IVortexHandoverStore store, IKnownItemsCatalogSource catalog)
    {
        _reader = reader;
        _store = store;
        _catalog = catalog;
    }

    public async Task<Result<IReadOnlyList<AddInsListChange>>> ExecuteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        if (!_catalog.Load().TryGetValue(out var catalog, out var error)
            || !(await _store.ReadKeptAddInsAsync(documentsDirectory, cancellationToken)).TryGetValue(out var kept, out error)
            || !(await _reader.ReadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var snapshot, out error))
        {
            return Result.Failure<IReadOnlyList<AddInsListChange>>(error);
        }

        return Result.Success(LostDlc.Find(kept, snapshot.RegisteredAddIns, catalog));
    }
}
