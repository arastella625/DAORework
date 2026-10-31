using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Backups;

/// <summary>
/// Takes the memento before a plan is applied (SDD §5, ROADMAP P2-04): every file the plan replaces or deletes as it
/// is now, the paths it creates as empty, and <c>AddIns.xml</c> when the plan edits it. A blocked or empty plan is not
/// backed up, because it is never applied.
/// </summary>
public sealed class BackUpDeployment
{
    /// <summary>Where <c>Settings\AddIns.xml</c> is, relative to the documents folder.</summary>
    public const string AddInsListPath = "Settings/AddIns.xml";

    private readonly IDeploymentBackupStore _store;

    public BackUpDeployment(IDeploymentBackupStore store)
    {
        _store = store;
    }

    public Task<Result<DeploymentBackup>> ExecuteAsync(BackUpDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plan = request.Plan;
        if (plan.IsBlocked)
        {
            return Task.FromResult(Result.Failure<DeploymentBackup>(new Error(DiagnosticCodes.ModPlanBlocked, "The plan is blocked, so it is not backed up or applied.")));
        }

        if (plan.IsEmpty)
        {
            return Task.FromResult(Result.Failure<DeploymentBackup>(new Error(DiagnosticCodes.ModPlanEmpty, "The plan changes nothing, so there is nothing to back up.")));
        }

        var targets = plan.Operations.Select(Target).ToList();
        if (plan.AddInsChanges.Count > 0)
        {
            targets.Add(new BackupTarget(AddInsListPath, ExpectedSha256: null, AfterSha256: null, AnyContent: true));
        }

        return _store.SaveAsync(request.DocumentsDirectory, targets, plan.CurrentRecord, cancellationToken);
    }

    private static BackupTarget Target(PlanOperation operation) =>
        operation.Action == PlanAction.Move
            ? new BackupTarget(operation.TargetPath, ExpectedSha256: null, AfterSha256: operation.ExpectedSha256) { MovedFrom = operation.MovedFrom }
            : new BackupTarget(operation.TargetPath, operation.ExpectedSha256, operation.Source?.Source.Sha256);
}
