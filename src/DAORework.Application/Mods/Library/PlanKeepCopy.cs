using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Library;

/// <summary>Works out what keeping one copy would do, with the plan that swaps a copy that is on (ADR-0039). Reads only.</summary>
public sealed class PlanKeepCopy
{
    private readonly IModLibrary _library;
    private readonly CopyFolders _folders;
    private readonly PlanTurnOn _plan;

    public PlanKeepCopy(IModLibrary library, CopyFolders folders, PlanTurnOn plan)
    {
        _library = library;
        _folders = folders;
        _plan = plan;
    }

    public async Task<Result<KeepCopyPreview>> ExecuteAsync(PlanKeepCopyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entries = await _library.ListAsync(cancellationToken);
        if (!(await _folders.ReadAsync(request.DocumentsDirectory, cancellationToken)).TryGetValue(out var folders, out var error))
        {
            return Result.Failure<KeepCopyPreview>(error);
        }

        var on = folders.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.DocumentsDirectory, request.DocumentsDirectory))?.ActiveModIds;
        if (Others(request, entries, on) is not { } others)
        {
            return Result.Failure<KeepCopyPreview>(new Error(DiagnosticCodes.ModNotInLibrary, "This mod has no other copy to remove."));
        }

        var plan = KeepOneCopy.Plan(entries.Single(e => e.Mod.Id == request.KeepId), others, request.DocumentsDirectory, folders);
        if (plan.SwappedOrder is null || plan.Problems.Count > 0 || request.DocumentsDirectory is not { } documents)
        {
            return Result.Success(new KeepCopyPreview(plan, null));
        }

        var change = new ModChange(ModChangeKind.Set, plan.SwappedOrder);
        return (await _plan.ExecuteAsync(new PlanModChangeRequest(documents, change, request.InstallDirectory), cancellationToken)).TryGetValue(out var swap, out error)
            ? Result.Success(new KeepCopyPreview(plan, swap))
            : Result.Failure<KeepCopyPreview>(error);
    }

    /// <summary>What goes: what was named, or else this mod's copies. Null when there is nothing to remove.</summary>
    private static List<LibraryEntry>? Others(PlanKeepCopyRequest request, IReadOnlyList<LibraryEntry> entries, IEnumerable<string>? on)
    {
        if (request.Removing is { } named)
        {
            return [.. entries.Where(e => named.Contains(e.Mod.Id, StringComparer.Ordinal))];
        }

        var copies = ModCopies.Find(entries, on);
        return copies.TryGetValue(request.KeepId, out var kept)
            ? [.. entries.Where(e => e.Mod.Id != request.KeepId && copies.TryGetValue(e.Mod.Id, out var copy) && copy.Group == kept.Group)]
            : null;
    }
}
