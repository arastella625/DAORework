using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Application.Tests.Mods.Apply;

public sealed class ApplyAndRecoverTests
{
    private const string Documents = "C:/Docs";

    private readonly InMemoryDeploymentStores _stores = new();
    private readonly FakeDeploymentBackupStore _backups = new();

    [Fact]
    public async Task Apply_BacksUpThenRunsEveryStepThenSavesTheNextRecordAndClosesTheJournal()
    {
        var plan = Plan();

        var report = (await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, plan))).Value;

        Assert.Equal("b0", report.BackupId);
        Assert.Equal(3, report.StepsRun);
        Assert.Equal(["Copy AddIns/fix_uid/x.ncs", "Copy packages/core/override/DAORework/a/a.dds", "AddIns.xml"], _stores.Written);
        Assert.Equal(["a", "fix"], _stores.Saved!.ActiveModIds);
        Assert.Equal([new RecordedAddIn("fix_uid", "fix")], _stores.Saved.AddedAddIns);
        Assert.Null(_stores.Journal);
    }

    [Fact]
    public async Task Apply_FailingStep_StopsKeepsTheJournalAndSavesNoRecord()
    {
        _stores.FailAtCall = 1;

        var result = await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, Plan()));

        Assert.Equal(DiagnosticCodes.ModApplyFailed, result.Error!.Code);
        Assert.StartsWith("Step 2 of 3 did not complete", result.Error.Message, StringComparison.Ordinal);
        Assert.Contains("backup b0", result.Error.Recommendation, StringComparison.Ordinal);
        Assert.Equal([0], _stores.Done);
        Assert.NotNull(_stores.Journal);
        Assert.Null(_stores.Saved);
    }

    [Fact]
    public async Task Apply_WhileAnEarlierOneIsUnfinished_IsRefusedBeforeAnyBackup()
    {
        _stores.FailAtCall = 0;
        await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, Plan()));

        var result = await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, Plan()));

        Assert.Equal(DiagnosticCodes.ModApplyUnfinished, result.Error!.Code);
        Assert.Single(_backups.Backups);
    }

    [Fact]
    public async Task Apply_EmptyPlan_FailsAtTheBackupWithoutAJournal()
    {
        var empty = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [], [], new Dictionary<string, string>(), [])).Value;

        var result = await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, empty));

        Assert.Equal(DiagnosticCodes.ModPlanEmpty, result.Error!.Code);
        Assert.Null(_stores.Journal);
    }

    [Fact]
    public async Task RecoverFinish_RunsOnlyTheStepsNotDone()
    {
        _stores.FailAtCall = 1;
        await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, Plan()));

        var report = (await Recover().ExecuteAsync(new RecoverModPlanRequest(Documents, RecoveryChoice.Finish))).Value;

        Assert.Equal(2, report.StepsRun);
        Assert.Equal(1, report.StepsAlreadyDone);
        Assert.Equal(["a", "fix"], _stores.Saved!.ActiveModIds);
        Assert.Null(_stores.Journal);
    }

    [Fact]
    public async Task RecoverRollBack_RestoresTheBackupAndKeepsThePreviousRecord()
    {
        _stores.FailAtCall = 2;
        await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, Plan()));

        var report = (await Recover().ExecuteAsync(new RecoverModPlanRequest(Documents, RecoveryChoice.RollBack))).Value;

        Assert.Equal("b0", _backups.Restored!.Id);
        Assert.Equal(0, report.StepsRun);
        Assert.Same(DeploymentRecord.Empty, _stores.Saved);
        Assert.Null(_stores.Journal);
    }

    [Fact]
    public async Task RecoverRollBack_BackupGone_FailsAndKeepsTheJournal()
    {
        _stores.FailAtCall = 0;
        await Apply().ExecuteAsync(new ApplyModPlanRequest(Documents, Plan()));
        _backups.Backups.Clear();

        var result = await Recover().ExecuteAsync(new RecoverModPlanRequest(Documents, RecoveryChoice.RollBack));

        Assert.Equal(DiagnosticCodes.ModBackupNotFound, result.Error!.Code);
        Assert.NotNull(_stores.Journal);
    }

    [Fact]
    public async Task Recover_NothingUnfinished_SaysSo()
    {
        var result = await Recover().ExecuteAsync(new RecoverModPlanRequest(Documents, RecoveryChoice.Finish));

        Assert.Equal(DiagnosticCodes.ModNothingToRecover, result.Error!.Code);
    }

    private static ModActivationPlan Plan() => ModActivationPlanner.Plan(new PlanInput(
        DeploymentRecord.Empty,
        [
            new LibraryMod("a", "A", ModKind.Override, [new LibraryModFile("a.dds", 1, "h1")]),
            new LibraryMod("fix", "Fix", ModKind.AddIn, [new LibraryModFile("Contents/addins/fix_uid/x.ncs", 1, "h2")], "fix_uid"),
        ],
        [],
        new Dictionary<string, string>(),
        [])).Value;

    private ApplyModPlan Apply() => new(new BackUpDeployment(_backups), _stores, Runner());

    private RecoverModPlan Recover() => new(_stores, Runner(), new RestoreDeployment(_backups), _stores);

    private JournaledApplyRunner Runner() => new(_stores, _stores, _stores);
}
