using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Troubleshooting;
using Microsoft.Extensions.Logging;

namespace DAORework.Application.Mods.Changes;

/// <summary>
/// Notes which mods a game was started with (ADR-0043). It only helps later, so a history that can't be read starts
/// again, and a search for the problem mod doesn't count, or its rounds would bury the list the player trusts.
/// </summary>
public sealed class RecordPlay
{
    private readonly IDeploymentRecordStore _records;
    private readonly IPlayHistoryStore _history;
    private readonly IProblemSearchStore _searches;
    private readonly TimeProvider _clock;
    private readonly ILogger<RecordPlay> _logger;

    public RecordPlay(IDeploymentRecordStore records, IPlayHistoryStore history, IProblemSearchStore searches, TimeProvider clock, ILogger<RecordPlay> logger)
    {
        _records = records;
        _history = history;
        _searches = searches;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        if ((await _searches.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var search, out _) && search is not null)
        {
            return Result.Success();
        }

        if (!(await _records.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var record, out var error))
        {
            _logger.LogWarning("This game's mods weren't noted: {Reason}", error.Message);
            return Result.Failure(error);
        }

        var loaded = await _history.LoadAsync(documentsDirectory, cancellationToken);
        if (!loaded.TryGetValue(out var history, out error))
        {
            _logger.LogWarning("Starting the list of recent games again: {Reason}", error.Message);
            history = PlayHistory.Empty;
        }

        try
        {
            await _history.SaveAsync(documentsDirectory, history.With(new PlayRecord(_clock.GetUtcNow(), record.ActiveModIds)), cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "This game's mods weren't noted");
            return Result.Failure(new Error(DiagnosticCodes.PlayHistoryUnreadable, $"The list of recent games couldn't be saved: {exception.Message}"));
        }
    }
}
