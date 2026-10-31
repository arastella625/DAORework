using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Backups;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;

namespace DAORework.Application.Mods.Apply;

/// <summary>
/// Applies a confirmed plan (SDD §5 Plan → Apply + Memento, ROADMAP P2-05): the backup first, then the journal, then
/// each step, then the new deployment record. If a step fails or the process dies, the journal stays behind and
/// <see cref="RecoverModPlan"/> finishes or rolls back; until then no other change is started in that folder.
/// </summary>
public sealed class ApplyModPlan
{
    private readonly BackUpDeployment _backUp;
    private readonly IApplyJournalStore _journals;
    private readonly JournaledApplyRunner _runner;

    public ApplyModPlan(BackUpDeployment backUp, IApplyJournalStore journals, JournaledApplyRunner runner)
    {
        _backUp = backUp;
        _journals = journals;
        _runner = runner;
    }

    public async Task<Result<ApplyReport>> ExecuteAsync(ApplyModPlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await _journals.LoadAsync(request.DocumentsDirectory, cancellationToken) is not null)
        {
            return Result.Failure<ApplyReport>(new Error(
                DiagnosticCodes.ModApplyUnfinished,
                $"An earlier change in {request.DocumentsDirectory} did not finish.",
                "Finish it or roll it back before making another change."));
        }

        if (!(await _backUp.ExecuteAsync(new BackUpDeploymentRequest(request.DocumentsDirectory, request.Plan), cancellationToken)).TryGetValue(out var backup, out var error))
        {
            return Result.Failure<ApplyReport>(error);
        }

        var plan = request.Plan;

        // The record names its backup, so a rollback undoes this change even if a later one never got past its backup.
        var journal = new ApplyJournal(request.DocumentsDirectory, backup.Id, plan.Operations, plan.AddInsChanges, plan.NextRecord with { BackupId = backup.Id }, plan.CurrentRecord);
        await _journals.StartAsync(journal, cancellationToken);
        return await _runner.RunAsync(journal, new HashSet<int>(), cancellationToken);
    }
}
