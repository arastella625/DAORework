using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Apply;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Fixes;

/// <summary>
/// Applies a confirmed fix plan (ADR-0013): stores a new entry in the library, then hands the plan to the same
/// backup, journal and executor as a mod change. A plan with nothing to change on disk only brings the record up to
/// date, for example after Steam's verify put back a file the fix had replaced.
/// </summary>
public sealed class ApplyFix
{
    private readonly IFixLibrary _library;
    private readonly ApplyModPlan _apply;
    private readonly IDeploymentRecordStore _records;

    public ApplyFix(IFixLibrary library, ApplyModPlan apply, IDeploymentRecordStore records)
    {
        _library = library;
        _apply = apply;
        _records = records;
    }

    public async Task<Result<FixApplyOutcome>> ExecuteAsync(ApplyFixRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var change = request.Planned.Plan.Change;
        if (change.IsBlocked)
        {
            return Result.Failure<FixApplyOutcome>(new Error(DiagnosticCodes.ModPlanBlocked, "The plan is blocked, so it is not applied."));
        }

        if (change.IsEmpty)
        {
            if (!SameRecord(change.CurrentRecord, change.NextRecord))
            {
                await _records.SaveAsync(request.InstallDirectory, change.NextRecord, cancellationToken);
            }

            return Result.Success(new FixApplyOutcome(null));
        }

        foreach (var (entry, contents) in ToStore(request.Planned.Prepared))
        {
            var stored = await _library.AddAsync(entry, contents, cancellationToken);
            if (stored.IsFailure)
            {
                return Result.Failure<FixApplyOutcome>(stored.Error!);
            }
        }

        return (await _apply.ExecuteAsync(new ApplyModPlanRequest(request.InstallDirectory, change), cancellationToken)).TryGetValue(out var report, out var failed)
            ? Result.Success(new FixApplyOutcome(report))
            : Result.Failure<FixApplyOutcome>(failed);
    }

    /// <summary>The entries a plan needs in the library before it runs: what the fix replaces first, then the fix itself.</summary>
    private static List<(FixEntry Entry, IReadOnlyDictionary<string, byte[]> Contents)> ToStore(PreparedFix? prepared)
    {
        var entries = new List<(FixEntry Entry, IReadOnlyDictionary<string, byte[]> Contents)>();
        if (prepared is null)
        {
            return entries;
        }

        entries.AddRange(prepared.AlsoKeep.Select(k => (k.Entry, k.Contents)));
        if (prepared.NewContents.Count > 0)
        {
            entries.Add((prepared.Entry, prepared.NewContents));
        }

        return entries;
    }

    private static bool SameRecord(DeploymentRecord a, DeploymentRecord b) =>
        a.ActiveModIds.SequenceEqual(b.ActiveModIds, StringComparer.OrdinalIgnoreCase) && a.Files.SequenceEqual(b.Files);
}
