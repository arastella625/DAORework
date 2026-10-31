using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ModActivationPlannerTests
{
    private const string HairPath = "packages/core/override/DAORework/hair/hair.dds";

    private readonly Dictionary<string, string> _disk = new(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Plan_FirstActivation_CopiesEveryFileAndNeedsTheirSize()
    {
        var plan = Plan(DeploymentRecord.Empty, Mods.Override("hair", "hair.dds#h1", "eyes.dds#e1"));

        Assert.Equal(2, plan.Count(PlanAction.Copy));
        Assert.All(plan.Operations, o => Assert.Null(o.ExpectedSha256));
        Assert.Equal(20, plan.BytesToWrite.Bytes);
        Assert.False(plan.IsBlocked);
        Assert.False(plan.IsEmpty);
    }

    [Fact]
    public void Plan_AlreadyDeployedAndUnchanged_IsEmpty()
    {
        _disk[HairPath] = "h1";

        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")), Mods.Override("hair", "hair.dds#h1"));

        Assert.True(plan.IsEmpty);
        Assert.Equal(1, plan.UnchangedFileCount);
    }

    [Fact]
    public void Plan_NewVersionOfARecordedUnchangedFile_ReplacesItExpectingTheRecordedHash()
    {
        _disk[HairPath] = "h1";

        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")), Mods.Override("hair", "hair.dds#h2"));

        var replace = Assert.Single(plan.Operations);
        Assert.Equal(PlanAction.Replace, replace.Action);
        Assert.Equal("h1", replace.ExpectedSha256);
    }

    [Fact]
    public void Plan_RecordedFileChangedByTheUser_BlocksInsteadOfReplacing()
    {
        _disk[HairPath] = "edited";

        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")), Mods.Override("hair", "hair.dds#h2"));

        var blocker = Assert.Single(plan.Blockers);
        Assert.Equal(PlanBlockerKind.ChangedSinceDeployed, blocker.Kind);
        Assert.Equal(HairPath, blocker.Subject);
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void Plan_RecordedFileAlreadyHoldingTheWantedContent_CountsAsUnchanged()
    {
        _disk[HairPath] = "h2";

        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")), Mods.Override("hair", "hair.dds#h2"));

        Assert.True(plan.IsEmpty);
        Assert.False(plan.IsBlocked);
    }

    [Fact]
    public void Plan_RecordedFileMissing_IsCopiedAgainAndReported()
    {
        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")), Mods.Override("hair", "hair.dds#h1"));

        Assert.Equal(PlanAction.Copy, Assert.Single(plan.Operations).Action);
        Assert.Equal([HairPath], plan.MissingRecordedFiles);
    }

    [Fact]
    public void Plan_FileDaoReworkDidNotPlaceIsInTheWay_BlocksAndIsNeverOverwritten()
    {
        _disk[HairPath] = "someone-elses";

        var plan = Plan(DeploymentRecord.Empty, Mods.Override("hair", "hair.dds#h1"));

        Assert.Equal(PlanBlockerKind.UnmanagedFileInTheWay, Assert.Single(plan.Blockers).Kind);
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void Plan_DeactivatingAMod_DeletesItsUnchangedFilesExpectingTheirHash()
    {
        _disk[HairPath] = "h1";

        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")));

        var delete = Assert.Single(plan.Operations);
        Assert.Equal(PlanAction.Delete, delete.Action);
        Assert.Equal("h1", delete.ExpectedSha256);
        Assert.Null(delete.Source);
        Assert.Equal(0, plan.BytesToWrite.Bytes);
    }

    [Fact]
    public void Plan_DeactivatingAModWhoseFileTheUserChanged_BlocksInsteadOfDeleting()
    {
        _disk[HairPath] = "edited";

        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")));

        Assert.Equal(PlanBlockerKind.ChangedSinceDeployed, Assert.Single(plan.Blockers).Kind);
        Assert.Empty(plan.Operations);
    }

    [Fact]
    public void Plan_DeactivatingAModWhoseFileIsAlreadyGone_HasNothingToDelete()
    {
        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")));

        Assert.True(plan.IsEmpty);
        Assert.Equal([HairPath], plan.MissingRecordedFiles);
    }

    [Fact]
    public void Plan_TwoModsWritingTheSameAddInFile_IsBlockedByThePathCollision()
    {
        var plan = Plan(DeploymentRecord.Empty, Mods.AddIn("a", "uid_a", "Contents/addins/shared/x.gda"), Mods.AddIn("b", "uid_b", "Contents/addins/shared/x.gda"));

        Assert.Contains(plan.Blockers, b => b.Kind == PlanBlockerKind.PathCollision && b.Subject == "AddIns/shared/x.gda");
    }

    [Fact]
    public void Plan_NewAddIn_RegistersItsUid()
    {
        var plan = Plan(DeploymentRecord.Empty, Mods.AddIn("fixpack", "qwinn_fixpack_3", "Contents/addins/qwinn_fixpack_3/x.ncs"));

        var change = Assert.Single(plan.AddInsChanges);
        Assert.Equal(new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: true), change);
    }

    [Fact]
    public void Plan_AddInAlreadyRegisteredByHand_BlocksRatherThanRegisteringItTwice()
    {
        var plan = PlanRegistered(DeploymentRecord.Empty, ["QWINN_FIXPACK_3"], Mods.AddIn("fixpack", "qwinn_fixpack_3", "Contents/addins/qwinn_fixpack_3/x.ncs"));

        Assert.Equal(PlanBlockerKind.AddInAlreadyRegistered, Assert.Single(plan.Blockers).Kind);
        Assert.Empty(plan.AddInsChanges);
    }

    [Fact]
    public void Plan_DeactivatingAnAddInDaoReworkAdded_RemovesOnlyThatEntry()
    {
        var record = new DeploymentRecord(["fixpack"], [], [new RecordedAddIn("qwinn_fixpack_3", "fixpack")]);

        var plan = PlanRegistered(record, ["qwinn_fixpack_3", "someone_elses"]);

        Assert.Equal([new AddInsListChange("qwinn_fixpack_3", "fixpack", Add: false)], plan.AddInsChanges);
    }

    [Fact]
    public void Plan_AddInEntryAlreadyRemovedByHand_IsNotRemovedAgain()
    {
        var record = new DeploymentRecord(["fixpack"], [], [new RecordedAddIn("qwinn_fixpack_3", "fixpack")]);

        Assert.True(PlanRegistered(record, []).IsEmpty);
    }

    [Fact]
    public void Plan_KeepsTheModOrderBeforeAndAfter()
    {
        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")), Mods.Override("b", "x.dds"), Mods.Override("a", "y.dds"));

        Assert.Equal(["hair"], plan.CurrentModIds);
        Assert.Equal(["b", "a"], plan.TargetModIds);
    }

    [Fact]
    public void Plan_OperationsComeDeletesAfterCopiesInPathOrder()
    {
        _disk[HairPath] = "h1";

        var plan = Plan(Record(new RecordedFile(HairPath, "h1", "hair")), Mods.Override("z", "b.dds", "a.dds"));

        Assert.Equal([PlanAction.Copy, PlanAction.Copy, PlanAction.Delete], plan.Operations.Select(o => o.Action));
        Assert.EndsWith("a.dds", plan.Operations[0].TargetPath, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_CarriesTheRecordBeforeAndTheRecordAfter()
    {
        var record = new DeploymentRecord(["old", "fix"], [], [new RecordedAddIn("old_uid", "old"), new RecordedAddIn("fix_uid", "fix")]);

        var plan = PlanRegistered(record, ["old_uid", "fix_uid"], Mods.AddIn("fix", "fix_uid", "Contents/addins/fix_uid/x.ncs#x1"), Mods.AddIn("new", "new_uid", "Contents/addins/new_uid/y.ncs#y1"));

        Assert.Same(record, plan.CurrentRecord);
        Assert.Equal(["fix", "new"], plan.NextRecord.ActiveModIds);
        Assert.Equal([new RecordedFile("AddIns/fix_uid/x.ncs", "x1", "fix"), new RecordedFile("AddIns/new_uid/y.ncs", "y1", "new")], plan.NextRecord.Files);
        Assert.Equal([new RecordedAddIn("fix_uid", "fix"), new RecordedAddIn("new_uid", "new")], plan.NextRecord.AddedAddIns);
    }

    [Fact]
    public void Plan_InvalidModList_FailsWithTheScannersError()
    {
        var result = ModActivationPlanner.Plan(Input(DeploymentRecord.Empty, [], [Mods.Override("a", "x.dds"), Mods.Override("a", "y.dds")]));

        Assert.Equal(DiagnosticCodes.ModListDuplicate, result.Error!.Code);
    }

    internal static DeploymentRecord Record(params RecordedFile[] files) =>
        new(files.Select(f => f.ModId).Distinct().ToList(), files, []);

    private ModActivationPlan Plan(DeploymentRecord record, params LibraryMod[] mods) => PlanRegistered(record, [], mods);

    private ModActivationPlan PlanRegistered(DeploymentRecord record, IReadOnlyCollection<string> registered, params LibraryMod[] mods) =>
        ModActivationPlanner.Plan(Input(record, registered, mods)).Value;

    private PlanInput Input(DeploymentRecord record, IReadOnlyCollection<string> registered, IReadOnlyList<LibraryMod> mods) =>
        new(record, mods, [], _disk, registered);
}
