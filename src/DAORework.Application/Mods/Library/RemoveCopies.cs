using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Mods.Library;

/// <summary>
/// Removes copies from the library for good (ADR-0039). What is on is checked again here, since the preview may be old:
/// a copy on anywhere, or a change that didn't finish, stops it before anything goes.
/// </summary>
public sealed class RemoveCopies
{
    private readonly IModRemover _remover;
    private readonly CopyFolders _folders;

    public RemoveCopies(IModRemover remover, CopyFolders folders)
    {
        _remover = remover;
        _folders = folders;
    }

    public async Task<Result> ExecuteAsync(IReadOnlyList<string> modIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modIds);
        if (!(await _folders.ReadAsync(null, cancellationToken)).TryGetValue(out var folders, out var error))
        {
            return Result.Failure(error);
        }

        if (folders.FirstOrDefault(f => f.Unfinished) is { } unfinished)
        {
            return Result.Failure(new Error(DiagnosticCodes.ModApplyUnfinished, $"A change in {unfinished.DocumentsDirectory} didn't finish.", "Finish it or undo it in History first."));
        }

        if (folders.FirstOrDefault(f => f.ActiveModIds.Intersect(modIds, StringComparer.OrdinalIgnoreCase).Any()) is { } on)
        {
            return Result.Failure(new Error(DiagnosticCodes.ModCopyStillOn, $"A copy is still on in {on.DocumentsDirectory}, so nothing was removed.", "Turn it off there first."));
        }

        foreach (var id in modIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var removed = _remover.Remove(id);
            if (removed.IsFailure)
            {
                return removed;
            }
        }

        return Result.Success();
    }
}
