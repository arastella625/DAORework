using DAORework.Application.Fixes;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Application.Tests.Fixes;

public sealed class FixUseCaseTests
{
    private const string Install = "D:/Games/Dragon Age";
    private const string Exe = LargeAddressAwareFix.ExecutablePath;

    private readonly InMemoryDeploymentStores _stores = new();
    private readonly FakeDeploymentBackupStore _backups = new();
    private readonly FakeFixLibrary _library = new();
    private readonly FakeInstallFiles _files = new();

    public FixUseCaseTests()
    {
        _files.Files[Exe] = TestExecutable.Build();
    }

    [Fact]
    public async Task PlanApply_ReplacesTheExeAndStoresNothingYet()
    {
        var planned = (await Plan(FixChangeKind.Apply)).Value;

        var operation = Assert.Single(planned.Plan.Change.Operations);
        Assert.Equal((PlanAction.Replace, Exe), (operation.Action, operation.TargetPath));
        Assert.Equal("4 GB patch", planned.FixName);
        Assert.Equal([LargeAddressAwareFix.PatchedPath, LargeAddressAwareFix.OriginalPath], planned.Prepared!.NewContents.Keys);
        Assert.Empty(_library.Entries);
        Assert.Null(_stores.Journal);
    }

    [Fact]
    public async Task Apply_StoresTheEntryThenRunsTheJournaledChange()
    {
        var planned = (await Plan(FixChangeKind.Apply)).Value;

        var outcome = (await ApplyFix().ExecuteAsync(new ApplyFixRequest(Install, planned))).Value;

        Assert.NotNull(outcome.Report);
        Assert.Equal([$"Replace {Exe}"], _stores.Written);
        Assert.Equal(planned.Plan.Change.Operations[0].Source!.ModId, Assert.Single(_stores.Saved!.ActiveModIds));
        Assert.Single(_library.Entries);
        Assert.True(PatchedIn(_library));
    }

    [Fact]
    public async Task Apply_LibraryCannotStoreTheEntry_ChangesNothing()
    {
        var planned = (await Plan(FixChangeKind.Apply)).Value;
        _library.FailAdd = true;

        var result = await ApplyFix().ExecuteAsync(new ApplyFixRequest(Install, planned));

        Assert.Equal(DiagnosticCodes.FixLibraryFailed, result.Error!.Code);
        Assert.Empty(_stores.Written);
        Assert.Null(_stores.Journal);
    }

    [Fact]
    public async Task PlanApply_WhenAppliedAlready_ChangesNothing()
    {
        await ApplyAndPatchTheFakeExe();

        var planned = (await Plan(FixChangeKind.Apply)).Value;

        Assert.True(planned.Plan.Change.IsEmpty);
        Assert.Equal(1, planned.Plan.Change.UnchangedFileCount);
    }

    [Fact]
    public async Task PlanApply_FlagSetOutsideDaorework_IsNotNeeded()
    {
        _files.Files[Exe] = TestExecutable.Build(largeAddressAware: true);

        Assert.Equal(DiagnosticCodes.FixNotNeeded, (await Plan(FixChangeKind.Apply)).Error!.Code);
    }

    [Fact]
    public async Task PlanApply_SignedExe_SaysTheSignatureWillNotVerify()
    {
        _files.Files[Exe] = TestExecutable.Build(securitySize: 0x2000);

        Assert.Contains("signature", Assert.Single((await Plan(FixChangeKind.Apply)).Value.Notes), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlanApply_NoExe_FailsWithTheReason()
    {
        _files.Files.Clear();

        Assert.Equal(DiagnosticCodes.FixInstallUnreadable, (await Plan(FixChangeKind.Apply)).Error!.Code);
    }

    [Theory]
    [InlineData(DiagnosticCodes.FixInstallNotWritable, PlanBlockerKind.InstallNotWritable)]
    [InlineData(DiagnosticCodes.FixFileInUse, PlanBlockerKind.FileInUse)]
    public async Task PlanApply_WhenTheInstallCannotBeChanged_IsBlockedAndNotApplied(string code, PlanBlockerKind kind)
    {
        _files.Problems.Add(new Error(code, "Cannot write.", "Do something."));

        var planned = (await Plan(FixChangeKind.Apply)).Value;
        var applied = await ApplyFix().ExecuteAsync(new ApplyFixRequest(Install, planned));

        var blocker = Assert.Single(planned.Plan.Change.Blockers);
        Assert.Equal((kind, "Cannot write. Do something."), (blocker.Kind, blocker.Message));
        Assert.Equal(DiagnosticCodes.ModPlanBlocked, applied.Error!.Code);
        Assert.Empty(_stores.Written);
    }

    [Fact]
    public async Task PlanRemove_PutsTheOriginalBack()
    {
        await ApplyAndPatchTheFakeExe();

        var planned = (await Plan(FixChangeKind.Remove, "4gb")).Value;

        var operation = Assert.Single(planned.Plan.Change.Operations);
        Assert.Equal((PlanAction.Replace, LargeAddressAwareFix.OriginalPath), (operation.Action, operation.Source!.Source.RelativePath));
        Assert.Empty(planned.Plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public async Task PlanRemove_NotApplied_Fails()
    {
        Assert.Equal(DiagnosticCodes.FixNotApplied, (await Plan(FixChangeKind.Remove)).Error!.Code);
    }

    [Fact]
    public async Task AfterSteamPutTheOriginalBack_StatusSaysSoAndRemovingOnlyUpdatesTheRecord()
    {
        await ApplyAndPatchTheFakeExe();
        _files.Files[Exe] = TestExecutable.Build();
        _stores.Written.Clear();

        var status = (await Status()).Value;
        var planned = (await Plan(FixChangeKind.Remove)).Value;
        var outcome = (await ApplyFix().ExecuteAsync(new ApplyFixRequest(Install, planned))).Value;

        Assert.Equal([Exe], Assert.Single(status.Applied).NotInEffect);
        Assert.Equal([Exe], planned.Plan.ChangedSince);
        Assert.Null(outcome.Report);
        Assert.Empty(_stores.Written);
        Assert.Empty(_stores.Saved!.ActiveModIds);
    }

    [Fact]
    public async Task Status_ListsWhatIsAppliedAndInEffect()
    {
        await ApplyAndPatchTheFakeExe();

        var status = (await Status()).Value;

        var applied = Assert.Single(status.Applied);
        Assert.True(applied.InEffect);
        Assert.Equal("4 GB patch", applied.FixName);
        Assert.False(status.ChangeUnfinished);
        Assert.Single(status.Backups);
    }

    [Fact]
    public async Task Status_FixNoLongerInTheLibrary_IsShownByItsId()
    {
        await _stores.SaveAsync(Install, new DeploymentRecord(["fixes/gone-12345678"], [], []));

        Assert.Equal("fixes/gone-12345678", Assert.Single((await Status()).Value.Applied).FixName);
    }

    [Fact]
    public async Task Plan_UnknownFix_ListsTheKnownOnes()
    {
        var result = await Plan(FixChangeKind.Apply, "reshade");

        Assert.Equal(DiagnosticCodes.FixUnknown, result.Error!.Code);
        Assert.Contains("laa (4 GB patch)", result.Error.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Plan_WhileAChangeIsUnfinished_PointsAtRecover()
    {
        await _stores.StartAsync(new ApplyJournal(Install, "b0", [], [], DeploymentRecord.Empty, DeploymentRecord.Empty));

        var result = await Plan(FixChangeKind.Apply);

        Assert.Equal(DiagnosticCodes.ModApplyUnfinished, result.Error!.Code);
        Assert.Contains("fixes recover", result.Error.Recommendation, StringComparison.Ordinal);
    }

    private static bool PatchedIn(FakeFixLibrary library) =>
        library.Contents.Keys.Any(k => k.EndsWith(LargeAddressAwareFix.PatchedPath, StringComparison.Ordinal));

    private async Task ApplyAndPatchTheFakeExe()
    {
        var planned = (await Plan(FixChangeKind.Apply)).Value;
        await ApplyFix().ExecuteAsync(new ApplyFixRequest(Install, planned));
        _files.Files[Exe] = _library.Contents.Single(c => c.Key.EndsWith(LargeAddressAwareFix.PatchedPath, StringComparison.Ordinal)).Value;
    }

    private Task<Result<PlannedFix>> Plan(FixChangeKind kind, string fix = "laa") =>
        new PlanFix(_stores, _stores, _library, _files, [new LargeAddressAwareFix(_files)]).ExecuteAsync(new PlanFixRequest(Install, fix, kind));

    private ApplyFix ApplyFix() =>
        new(_library, new ApplyModPlan(new BackUpDeployment(_backups), _stores, new JournaledApplyRunner(_stores, _stores, _stores)), _stores);

    private Task<Result<FixStatus>> Status() => new GetFixStatus(_stores, _stores, _backups, _library, _files).ExecuteAsync(Install);
}
