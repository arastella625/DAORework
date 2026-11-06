using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Troubleshooting;

namespace DAORework.Application.Mods.Changes;

/// <summary>What is different from the last game with other mods, with the time now, so the window can say when (ADR-0043).</summary>
public sealed class ReadPlayChanges
{
    private readonly IDeploymentRecordStore _records;
    private readonly IPlayHistoryStore _history;
    private readonly TimeProvider _clock;

    public ReadPlayChanges(IDeploymentRecordStore records, IPlayHistoryStore history, TimeProvider clock)
    {
        _records = records;
        _history = history;
        _clock = clock;
    }

    public async Task<Result<(PlayChanges? Changes, DateTimeOffset Now)>> ExecuteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        if (!(await _records.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var record, out var error)
            || !(await _history.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var history, out error))
        {
            return Result.Failure<(PlayChanges?, DateTimeOffset)>(error);
        }

        return Result.Success<(PlayChanges?, DateTimeOffset)>((history.ChangesSince(record.ActiveModIds), _clock.GetLocalNow()));
    }
}
