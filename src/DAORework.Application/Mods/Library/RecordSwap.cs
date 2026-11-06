using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Library;

/// <summary>
/// A swap that moves no file (ADR-0039): a copy that is on owns nothing when another copy placed the same files first, so
/// taking it out only changes the record. With nothing to restore there is no backup, and the record keeps pointing at the
/// last one. <see cref="CheckAsync"/> comes first for any swap: the list stays usable under a preview, and a plan from
/// before another change would drop that change from the record.
/// </summary>
public sealed class RecordSwap
{
    private readonly IDeploymentRecordStore _records;
    private readonly IApplyJournalStore _journals;

    public RecordSwap(IDeploymentRecordStore records, IApplyJournalStore journals)
    {
        _records = records;
        _journals = journals;
    }

    /// <summary>Whether <paramref name="plan"/> still starts from what is on, with no change left unfinished.</summary>
    public async Task<Result> CheckAsync(string documentsDirectory, ModActivationPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (await _journals.LoadAsync(documentsDirectory, cancellationToken) is not null)
        {
            return Result.Failure(new Error(DiagnosticCodes.ModApplyUnfinished, $"A change in {documentsDirectory} didn't finish.", "Finish it or undo it in History first."));
        }

        if (!(await _records.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Result.Failure(error);
        }

        if (!record.ActiveModIds.SequenceEqual(plan.CurrentModIds, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(new Error(DiagnosticCodes.ModCopyStillOn, "What's on has changed since this preview.", "Open the preview again."));
        }

        return Result.Success();
    }

    /// <summary>Saves the record of a checked plan that moves no file.</summary>
    public Task ExecuteAsync(string documentsDirectory, ModActivationPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return _records.SaveAsync(documentsDirectory, plan.NextRecord with { BackupId = plan.CurrentRecord.BackupId }, cancellationToken);
    }
}
