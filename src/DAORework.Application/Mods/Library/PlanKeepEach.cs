using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Library;

/// <summary>Works out what keeping one copy of every mod would do, with the one plan for any swaps (ADR-0039). Reads only.</summary>
public sealed class PlanKeepEach
{
    private readonly IModLibrary _library;
    private readonly CopyFolders _folders;
    private readonly PlanTurnOn _plan;

    public PlanKeepEach(IModLibrary library, CopyFolders folders, PlanTurnOn plan)
    {
        _library = library;
        _folders = folders;
        _plan = plan;
    }

    public async Task<Result<KeepEachPreview>> ExecuteAsync(PlanKeepEachRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entries = await _library.ListAsync(cancellationToken);
        if (!(await _folders.ReadAsync(request.DocumentsDirectory, cancellationToken)).TryGetValue(out var folders, out var error))
        {
            return Result.Failure<KeepEachPreview>(error);
        }

        var plan = KeepOneOfEach.Plan(entries, request.Picks, request.DocumentsDirectory, folders);
        if (plan.SwappedOrder is null || plan.Problems.Count > 0 || request.DocumentsDirectory is not { } documents)
        {
            return Result.Success(new KeepEachPreview(plan, null));
        }

        var change = new ModChange(ModChangeKind.Set, plan.SwappedOrder);
        return (await _plan.ExecuteAsync(new PlanModChangeRequest(documents, change, request.InstallDirectory), cancellationToken)).TryGetValue(out var swap, out error)
            ? Result.Success(new KeepEachPreview(plan, swap))
            : Result.Failure<KeepEachPreview>(error);
    }
}
