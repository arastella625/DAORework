using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Common;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Backups;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// What the window offers for a folder DAORework changes through the plan, backup and journal path (the documents
/// folder for mods, the install folder for fixes, ADR-0013): the backups to roll back to, each asked for once more
/// before anything is put back, and the way out of a change that did not finish. As <c>rollback</c> and
/// <c>recover</c> do in the CLI, with the same use cases.
/// </summary>
public abstract partial class RecoverableChangesViewModel : PageViewModel
{
    private const int MaxBackups = 10;

    private readonly RollBackModChange _rollBack;
    private readonly RecoverModPlan _recover;
    private (string Folder, string BackupId)? _pending;

    protected RecoverableChangesViewModel(RollBackModChange rollBack, RecoverModPlan recover, ILogger logger)
        : base(logger)
    {
        _rollBack = rollBack;
        _recover = recover;
    }

    /// <summary>Gets or sets what runs after a rollback or a recovery, such as reading another panel again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets the backups, most recent first.</summary>
    public ObservableCollection<BackupRow> Backups { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial bool ChangeUnfinished { get; set; }

    [ObservableProperty]
    public partial string? PendingRollback { get; set; }

    /// <summary>Gets what a rollback brings back, said before it is confirmed.</summary>
    protected abstract string RollbackConsequence { get; }

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        ClearPending();
        ReportSuccess(string.Empty);
        return RefreshAsync(cancellationToken);
    }

    /// <summary>Reads the folder's record, journal and backups again.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) => GuardAsync(ReadAsync, cancellationToken);

    /// <summary>The folder the changes are made in, which the profile must name.</summary>
    protected abstract Task<Result<string>> FolderAsync(CancellationToken cancellationToken);

    /// <summary>Reads the status into the panel; a folder the profile does not name has nothing to show, which is not an error.</summary>
    protected abstract Task ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Shows the backups that record what was active before them, most recent first, as <see cref="BackupRow.From"/>
    /// words them. What came after a backup's change is what the next newer one had before it, and for the newest, what
    /// is active now (P2.5-19).
    /// </summary>
    protected void ShowBackups(IReadOnlyList<DeploymentBackup> backups, IReadOnlyList<string> active, ChangeWords words, Func<string, string> name, string none)
    {
        ArgumentNullException.ThrowIfNull(backups);
        Backups.Clear();
        IReadOnlyList<string>? after = active;
        foreach (var backup in backups)
        {
            if (Backups.Count == MaxBackups)
            {
                break;
            }

            if (BackupRow.From(backup, after, words, name, none) is { } row)
            {
                Backups.Add(row);
            }

            after = backup.PreviousRecord?.ActiveModIds;
        }
    }

    [RelayCommand]
    private Task RollBackToAsync(BackupRow backup, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                ClearPending();
                if (!(await FolderAsync(token)).TryGetValue(out var folder, out var error))
                {
                    ReportError(error);
                    return;
                }

                _pending = (folder, backup.Id);
                var what = backup.Change is null ? $"the change backed up as {backup.Id}" : $"\u201c{backup.Change}\u201d";
                PendingRollback = $"Undo back to before {what}? The files it changed are put back as they were, and {RollbackConsequence}";
                ReportSuccess(string.Empty);
            },
            cancellationToken);

    [RelayCommand]
    private Task ConfirmRollbackAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (_pending is not { } pending)
                {
                    return;
                }

                ClearPending();
                var result = await _rollBack.ExecuteAsync(new RollBackModChangeRequest(pending.Folder, pending.BackupId), token);
                await AfterAsync(token);
                if (!result.TryGetValue(out var report, out var error))
                {
                    ReportError(error with { Message = $"Couldn't undo it: {error.Message}" });
                    return;
                }

                var left = report.LeftInPlace.Count == 0 ? string.Empty : $" Left as they are, since they changed afterwards: {string.Join(", ", report.LeftInPlace)}.";
                ReportSuccess($"Undone: {Counted.Of(report.Restored, "file")} put back, {report.Removed} removed.{left}");
            },
            cancellationToken);

    [RelayCommand]
    private void CancelRollback()
    {
        ClearPending();
        ReportSuccess("Nothing was changed.");
    }

    [RelayCommand]
    private Task FinishInterruptedAsync(CancellationToken cancellationToken) => RecoverAsync(RecoveryChoice.Finish, cancellationToken);

    [RelayCommand]
    private Task RollBackInterruptedAsync(CancellationToken cancellationToken) => RecoverAsync(RecoveryChoice.RollBack, cancellationToken);

    private Task RecoverAsync(RecoveryChoice choice, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                ClearPending();
                if (!(await FolderAsync(token)).TryGetValue(out var folder, out var error))
                {
                    ReportError(error);
                    return;
                }

                var result = await _recover.ExecuteAsync(new RecoverModPlanRequest(folder, choice), token);
                await AfterAsync(token);
                if (!result.TryGetValue(out var report, out error))
                {
                    ReportError(error with { Message = $"Couldn't settle the unfinished change: {error.Message}" });
                    return;
                }

                ReportSuccess(choice == RecoveryChoice.Finish
                    ? "Finished the change that was cut short."
                    : "Undid the change that was cut short. Your game is as it was before it.");
            },
            cancellationToken);

    private async Task AfterAsync(CancellationToken cancellationToken)
    {
        await ReadAsync(cancellationToken);
        if (AfterChangeAsync is not null)
        {
            await AfterChangeAsync(cancellationToken);
        }
    }

    private void ClearPending()
    {
        _pending = null;
        PendingRollback = null;
    }
}
