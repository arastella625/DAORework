using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;

namespace DAORework.Application.Mods.Apply;

/// <summary>
/// Runs a journal's remaining steps in order, logging each as done, then saves the target record and closes the
/// journal (ROADMAP P2-05). Shared by <see cref="ApplyModPlan"/> and the Finish choice of <see cref="RecoverModPlan"/>:
/// every step is safe to repeat, so running from a crash's journal is the same as running fresh.
/// </summary>
public sealed class JournaledApplyRunner
{
    private readonly IDeploymentWriter _writer;
    private readonly IApplyJournalStore _journals;
    private readonly IDeploymentRecordStore _records;

    public JournaledApplyRunner(IDeploymentWriter writer, IApplyJournalStore journals, IDeploymentRecordStore records)
    {
        _writer = writer;
        _journals = journals;
        _records = records;
    }

    public async Task<Result<ApplyReport>> RunAsync(ApplyJournal journal, IReadOnlySet<int> doneSteps, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(doneSteps);
        var documents = journal.DocumentsDirectory;
        var ran = 0;
        for (var step = 0; step < journal.StepCount; step++)
        {
            if (doneSteps.Contains(step))
            {
                continue;
            }

            var result = step < journal.Operations.Count
                ? await _writer.ApplyAsync(documents, journal.Operations[step], cancellationToken)
                : await _writer.EditAddInsListAsync(documents, journal.AddInsChanges, cancellationToken);
            if (result.IsFailure)
            {
                return Result.Failure<ApplyReport>(new Error(
                    DiagnosticCodes.ModApplyFailed,
                    $"Step {step + 1} of {journal.StepCount} did not complete: {result.Error!.Message}",
                    $"Nothing is lost. Finish the change, or roll it back to backup {journal.BackupId}."));
            }

            await _journals.MarkDoneAsync(documents, step, cancellationToken);
            ran++;
        }

        await _records.SaveAsync(documents, journal.Target, cancellationToken);
        await _journals.DeleteAsync(documents, cancellationToken);
        return Result.Success(new ApplyReport(journal.BackupId, ran, doneSteps.Count));
    }
}
