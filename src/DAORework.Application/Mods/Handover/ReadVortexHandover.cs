using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Handover;

namespace DAORework.Application.Mods.Handover;

/// <summary>Works out where the handover from Vortex stands in a documents folder (ADR-0028 §3). Only reads.</summary>
public sealed class ReadVortexHandover
{
    private readonly IHandInstalledModsReader _reader;
    private readonly IVortexHandoverStore _store;
    private readonly IDeploymentRecordStore _records;
    private readonly FindLostDlc _findLostDlc;

    public ReadVortexHandover(IHandInstalledModsReader reader, IVortexHandoverStore store, IDeploymentRecordStore records, FindLostDlc findLostDlc)
    {
        _findLostDlc = findLostDlc;
        _reader = reader;
        _store = store;
        _records = records;
    }

    public async Task<Result<VortexHandoverState>> ExecuteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        if (!(await _reader.ReadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var snapshot, out var error)
            || !(await _store.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var handover, out error)
            || !(await _records.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var record, out error)
            || !(await _findLostDlc.ExecuteAsync(documentsDirectory, cancellationToken)).TryGetValue(out var lost, out error))
        {
            return Result.Failure<VortexHandoverState>(error);
        }

        return Result.Success(VortexHandoverState.From(snapshot, handover, record.ActiveModIds, lost));
    }
}
