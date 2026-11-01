using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The Mods tab's <i>Changes</i> (P3-04): status, rollback to a chosen backup once confirmed, and recovery, as in the CLI.</summary>
public sealed class ModChangesScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public ModChangesScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Backups_EachSayWhatTheirChangeDid_NewestFirst()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);
        await viewModel.ModsTab.Activation.DeactivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack"));
        await viewModel.ModsTab.Activation.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(["Turned off Hair Pack", "Turned on Hair Pack"], viewModel.ModsTab.Changes.Backups.Select(b => b.Change));
    }

    [AvaloniaFact]
    public async Task RollBack_AskedOnceMore_ThenPutsTheFolderBack()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);
        var changes = viewModel.ModsTab.Changes;

        // Mods are named by their Library names, and each backup says what its change did (P2.5-19).
        Assert.Equal("On now, in order: Hair Pack (1 file).", changes.Summary);
        var backup = Assert.Single(changes.Backups);
        Assert.Equal("Turned on Hair Pack", backup.Change);
        Assert.Equal("Before it: no mods", backup.Before);

        await changes.RollBackToCommand.ExecuteAsync(backup);

        Assert.StartsWith("Undo back to before \u201cTurned on Hair Pack\u201d? ", changes.PendingRollback, StringComparison.Ordinal);
        Assert.Single(_scenario.DeployedHair());
        ModsTabScenario.Show<ChangesPanel>(window);
        Screenshots.Save(window, "15-changes-rollback");

        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.False(changes.IsError, changes.StatusMessage);
        Assert.Equal("Undone: 0 files put back, 1 removed.", changes.StatusMessage);
        Assert.Null(changes.PendingRollback);
        Assert.Empty(_scenario.DeployedHair());
        Assert.False(viewModel.Library.Mods[0].IsActive);
        Assert.Equal("No mods are on yet.", changes.Summary);
    }

    [AvaloniaFact]
    public async Task RollBack_Cancelled_ChangesNothing()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);
        var changes = viewModel.ModsTab.Changes;
        await changes.RollBackToCommand.ExecuteAsync(changes.Backups[0]);

        changes.CancelRollbackCommand.Execute(null);
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.Equal("Nothing was changed.", changes.StatusMessage);
        Assert.Single(_scenario.DeployedHair());
        Assert.True(viewModel.Library.Mods[0].IsActive);
    }

    [AvaloniaFact]
    public async Task InterruptedChange_Finished_CompletesIt()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await InterruptActivationAsync(viewModel);
        var changes = viewModel.ModsTab.Changes;

        Assert.True(changes.ChangeUnfinished);

        await changes.FinishInterruptedCommand.ExecuteAsync(null);

        Assert.False(changes.IsError, changes.StatusMessage);
        Assert.Equal("Finished the change that was cut short.", changes.StatusMessage);
        Assert.False(changes.ChangeUnfinished);
        Assert.Single(_scenario.DeployedHair());
        Assert.True(viewModel.Library.Mods[0].IsActive);
    }

    [AvaloniaFact]
    public async Task InterruptedChange_RolledBack_LeavesTheFolderAsBefore()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var backupId = await InterruptActivationAsync(viewModel);
        var changes = viewModel.ModsTab.Changes;

        await changes.RollBackInterruptedCommand.ExecuteAsync(null);

        Assert.False(changes.IsError, changes.StatusMessage);
        Assert.Equal("Undid the change that was cut short. Your game is as it was before it.", changes.StatusMessage);
        Assert.False(changes.ChangeUnfinished);
        Assert.Empty(_scenario.DeployedHair());
        Assert.False(viewModel.Library.Mods[0].IsActive);
    }

    /// <summary>What <c>ApplyModPlan</c> leaves when the process dies after the journal is written: the backup and the journal, no step run.</summary>
    private async Task<string> InterruptActivationAsync(MainWindowViewModel viewModel)
    {
        var token = TestContext.Current.CancellationToken;
        var documents = _scenario.DocumentsDirectory;
        var plan = (await _host.Resolve<PlanModChange>().ExecuteAsync(new PlanModChangeRequest(documents, new ModChange(ModChangeKind.Activate, [viewModel.Library.Mods[0].Id])), token)).Value;
        var backup = (await _host.Resolve<BackUpDeployment>().ExecuteAsync(new BackUpDeploymentRequest(documents, plan), token)).Value;
        await _host.Resolve<IApplyJournalStore>().StartAsync(
            new ApplyJournal(documents, backup.Id, plan.Operations, plan.AddInsChanges, plan.NextRecord with { BackupId = backup.Id }, plan.CurrentRecord),
            token);
        await viewModel.ModsTab.Changes.RefreshAsync(token);
        return backup.Id;
    }
}
