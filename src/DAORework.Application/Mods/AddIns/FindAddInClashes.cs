using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.AddIns;

namespace DAORework.Application.Mods.AddIns;

/// <summary>
/// Lists the add-ins the game loads from a documents folder, in its order, and the files they share (P2.5-23). Only
/// reads: DAORework changes no priority.
/// </summary>
public sealed class FindAddInClashes
{
    private readonly IDocumentsAddInsReader _reader;

    public FindAddInClashes(IDocumentsAddInsReader reader)
    {
        _reader = reader;
    }

    /// <returns>The report. No documents folder known (empty) is no add-ins.</returns>
    public async Task<Result<AddInsReport>> ExecuteAsync(string? documentsDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentsDirectory))
        {
            return Result.Success(new AddInsReport([], [], []));
        }

        if (!(await _reader.ReadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var addIns, out var error))
        {
            return Result.Failure<AddInsReport>(error);
        }

        return Result.Success(new AddInsReport(AddInRanking.LoadOrder(addIns.Listed), AddInClashes.Find(addIns.Contents), addIns.Unreadable));
    }
}
