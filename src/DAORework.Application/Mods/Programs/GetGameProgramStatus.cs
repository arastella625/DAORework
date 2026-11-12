using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Fixes;

namespace DAORework.Application.Mods.Programs;

/// <summary>
/// Which mods that run inside the game are on in an install folder (ADR-0033 §4): the entries of the folder's record
/// that aren't fixes, which share it. Reads only.
/// </summary>
public sealed class GetGameProgramStatus
{
    private readonly IDeploymentRecordStore _records;

    public GetGameProgramStatus(IDeploymentRecordStore records)
    {
        _records = records;
    }

    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(string installDirectory, CancellationToken cancellationToken = default) =>
        (await _records.LoadAsync(installDirectory, cancellationToken)).TryGetValue(out var record, out var error)
            ? Result.Success<IReadOnlyList<string>>([.. record.ActiveModIds.Where(id => !FixIds.IsFixEntry(id))])
            : Result.Failure<IReadOnlyList<string>>(error);
}
