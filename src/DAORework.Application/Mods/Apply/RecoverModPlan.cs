using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Backups;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;

namespace DAORework.Application.Mods.Apply;

/// <summary>
/// Settles a change that did not finish (ROADMAP P2-05): Finish runs the journal's remaining steps; RollBack restores
/// the change's backup and keeps the record from before it. Either way the folder ends in the before- or the
/// after-state of the plan, never a mix of the two.
/// </summary>
public sealed class RecoverModPlan
{
    private readonly IApplyJournalStore _journals;
    private readonly JournaledApplyRunner _runner;
    private readonly RestoreDeployment _restore;
    private readonly IDeploymentRecordStore _records;

    public RecoverModPlan(IApplyJournalStore journals, JournaledApplyRunner runner, RestoreDeployment restore, IDeploymentRecordStore records)
    {
        _journals = journals;
        _runner = runner;
        _restore = restore;
        _records = records;
    }

    public async Task<Result<ApplyReport>> ExecuteAsync(RecoverModPlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var progress = await _journals.LoadAsync(request.DocumentsDirectory, cancellationToken);
        if (progress is null)
        {
            return Result.Failure<ApplyReport>(new Error(DiagnosticCodes.ModNothingToRecover, $"There is no unfinished change in {request.DocumentsDirectory}."));
        }

        if (request.Choice == RecoveryChoice.Finish)
        {
            return await _runner.RunAsync(progress.Journal, progress.DoneSteps, cancellationToken);
        }

        var journal = progress.Journal;
        var restored = await _restore.ExecuteAsync(new RestoreDeploymentRequest(request.DocumentsDirectory, journal.BackupId), cancellationToken);
        if (restored.IsFailure)
        {
            return Result.Failure<ApplyReport>(restored.Error!);
        }

        await _records.SaveAsync(request.DocumentsDirectory, journal.Previous, cancellationToken);
        await _journals.DeleteAsync(request.DocumentsDirectory, cancellationToken);
        return Result.Success(new ApplyReport(journal.BackupId, 0, progress.DoneSteps.Count));
    }
}
