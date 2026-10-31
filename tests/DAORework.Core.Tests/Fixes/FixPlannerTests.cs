using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Fixes;

public sealed class FixPlannerTests
{
    private const string Exe = "bin_ship/daorigins.exe";
    private const string D3d9 = "bin_ship/d3d9.dll";

    private static readonly FixEntry _laa = new(
        "large-address-aware-aaaaaaaa",
        FixIds.LargeAddressAware,
        null,
        [new FixFile(Exe, new LibraryModFile("patched/daorigins.exe", 10, "patched"))],
        [new LibraryModFile("original/daorigins.exe", 10, "original")]);

    private static readonly FixEntry _dxvkOld = Dxvk("dxvk-2-2-bbbbbbbb", "dxvk22");
    private static readonly FixEntry _dxvkNew = Dxvk("dxvk-2-3-cccccccc", "dxvk23");

    [Fact]
    public void Apply_OnTheUnpatchedExe_ReplacesItAndRecordsWhatItReplaced()
    {
        var plan = FixPlanner.PlanApply(_laa, State(DeploymentRecord.Empty, (Exe, "original"), replaceable: "ORIGINAL"));

        var operation = Assert.Single(plan.Change.Operations);
        Assert.Equal((PlanAction.Replace, "original", "patched/daorigins.exe"), (operation.Action, operation.ExpectedSha256, operation.Source!.Source.RelativePath));
        Assert.Equal("fixes/large-address-aware-aaaaaaaa", operation.Source.ModId);
        var recorded = Assert.Single(plan.Change.NextRecord.Files);
        Assert.Equal(("patched", "original"), (recorded.Sha256, recorded.BeforeSha256));
        Assert.Equal(["fixes/large-address-aware-aaaaaaaa"], plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public void Apply_OverAFileItWasNotMadeFor_IsBlocked()
    {
        var plan = FixPlanner.PlanApply(_laa, State(DeploymentRecord.Empty, (Exe, "someone-elses")));

        Assert.Empty(plan.Change.Operations);
        Assert.Equal(PlanBlockerKind.UnmanagedFileInTheWay, Assert.Single(plan.Change.Blockers).Kind);
    }

    [Fact]
    public void Apply_AlreadyApplied_ChangesNothingAndKeepsTheRecord()
    {
        var record = Record(_laa, new RecordedFile(Exe, "patched", _laa.LibraryId) { BeforeSha256 = "original" });

        var plan = FixPlanner.PlanApply(_laa, State(record, (Exe, "patched")));

        Assert.True(plan.Change.IsEmpty);
        Assert.Equal(1, plan.Change.UnchangedFileCount);
        Assert.Equal(record.Files, plan.Change.NextRecord.Files);
        Assert.Equal(record.ActiveModIds, plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public void Apply_AlreadyPatchedByHand_IsNotClaimed()
    {
        var plan = FixPlanner.PlanApply(_laa, State(DeploymentRecord.Empty, (Exe, "patched")));

        Assert.True(plan.Change.IsEmpty);
        Assert.Empty(plan.Change.NextRecord.Files);
        Assert.Empty(plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public void Remove_PutsTheKeptOriginalBack()
    {
        var record = Record(_laa, new RecordedFile(Exe, "patched", _laa.LibraryId) { BeforeSha256 = "original" });

        var plan = FixPlanner.PlanRemove(_laa.LibraryId, State(record, (Exe, "patched")), [_laa]);

        var operation = Assert.Single(plan.Change.Operations);
        Assert.Equal((PlanAction.Replace, "patched", "original/daorigins.exe"), (operation.Action, operation.ExpectedSha256, operation.Source!.Source.RelativePath));
        Assert.Equal((FixChangeKind.Remove, "large-address-aware-aaaaaaaa"), (plan.Kind, plan.EntryId));
        Assert.Empty(plan.Change.NextRecord.Files);
        Assert.Empty(plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public void Remove_AfterSteamPutTheOriginalBack_LeavesItAndDropsTheRecord()
    {
        var record = Record(_laa, new RecordedFile(Exe, "patched", _laa.LibraryId) { BeforeSha256 = "original" });

        var plan = FixPlanner.PlanRemove(_laa.LibraryId, State(record, (Exe, "original")), [_laa]);

        Assert.Empty(plan.Change.Operations);
        Assert.Equal([Exe], plan.ChangedSince);
        Assert.Empty(plan.Change.NextRecord.Files);
    }

    [Fact]
    public void Remove_OriginalNoLongerInTheLibrary_IsBlocked()
    {
        var record = Record(_laa, new RecordedFile(Exe, "patched", _laa.LibraryId) { BeforeSha256 = "original" });

        var plan = FixPlanner.PlanRemove(_laa.LibraryId, State(record, (Exe, "patched")), []);

        Assert.Equal(PlanBlockerKind.FixOriginalMissing, Assert.Single(plan.Change.Blockers).Kind);
    }

    [Fact]
    public void Dxvk_CopiedWhereNothingWasAndDeletedOnRemoval()
    {
        var applied = FixPlanner.PlanApply(_dxvkNew, State(DeploymentRecord.Empty));
        var removed = FixPlanner.PlanRemove(_dxvkNew.LibraryId, State(applied.Change.NextRecord, (D3d9, "dxvk23")), [_dxvkNew]);

        Assert.Equal(PlanAction.Copy, Assert.Single(applied.Change.Operations).Action);
        Assert.Null(Assert.Single(applied.Change.NextRecord.Files).BeforeSha256);
        Assert.Equal((PlanAction.Delete, "dxvk23"), (Assert.Single(removed.Change.Operations).Action, removed.Change.Operations[0].ExpectedSha256));
        Assert.Empty(removed.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public void Dxvk_Upgrade_ReplacesTheOldEntryAndKeepsWhatWasThereBeforeDaorework()
    {
        var record = Record(_dxvkOld, new RecordedFile(D3d9, "dxvk22", _dxvkOld.LibraryId));

        var plan = FixPlanner.PlanApply(_dxvkNew, State(record, (D3d9, "dxvk22")));

        Assert.Equal((PlanAction.Replace, "dxvk22"), (Assert.Single(plan.Change.Operations).Action, plan.Change.Operations[0].ExpectedSha256));
        Assert.Null(Assert.Single(plan.Change.NextRecord.Files).BeforeSha256);
        Assert.Equal([_dxvkNew.LibraryId], plan.Change.NextRecord.ActiveModIds);
    }

    [Fact]
    public void Apply_RecordedFileGone_IsNotedAndPlacedAgain()
    {
        var record = Record(_dxvkNew, new RecordedFile(D3d9, "dxvk23", _dxvkNew.LibraryId));

        var plan = FixPlanner.PlanApply(_dxvkNew, State(record));

        Assert.Equal([D3d9], plan.Change.MissingRecordedFiles);
        Assert.Equal(PlanAction.Copy, Assert.Single(plan.Change.Operations).Action);
    }

    [Fact]
    public void Apply_KeepsOtherFixesInTheRecord()
    {
        var record = Record(_laa, new RecordedFile(Exe, "patched", _laa.LibraryId) { BeforeSha256 = "original" });

        var plan = FixPlanner.PlanApply(_dxvkNew, State(record, (Exe, "patched")));

        Assert.Equal([_laa.LibraryId, _dxvkNew.LibraryId], plan.Change.NextRecord.ActiveModIds);
        Assert.Equal([Exe, D3d9], plan.Change.NextRecord.Files.Select(f => f.TargetPath));
    }

    [Fact]
    public void BlockersFoundUpFront_BlockThePlan()
    {
        var notWritable = new PlanBlocker(PlanBlockerKind.InstallNotWritable, "bin_ship", "no rights");
        var state = State(DeploymentRecord.Empty) with { Blockers = [notWritable] };

        Assert.True(FixPlanner.PlanApply(_dxvkNew, state).Change.IsBlocked);
    }

    [Fact]
    public void EntryIds_AreMadeLikeModIdsAndLiveUnderFixes()
    {
        Assert.Equal("dxvk-2-3-abcdef12", FixIds.CreateEntryId(FixIds.Dxvk, "2.3", "ABCDEF1234"));
        Assert.Equal("large-address-aware-abcdef12", FixIds.CreateEntryId(FixIds.LargeAddressAware, null, "abcdef1234"));
        Assert.Equal("fixes/dxvk-2-3-abcdef12", FixIds.LibraryId("dxvk-2-3-abcdef12"));
    }

    private static FixEntry Dxvk(string id, string sha) => new(id, FixIds.Dxvk, "2.x", [new FixFile(D3d9, new LibraryModFile("d3d9.dll", 5, sha))], []);

    private static DeploymentRecord Record(FixEntry entry, RecordedFile file) => new([entry.LibraryId], [file], []);

    private static InstallState State(DeploymentRecord record, (string Path, string Sha)? file = null, string? replaceable = null) =>
        new(
            record,
            file is { } f ? new Dictionary<string, string> { [f.Path] = f.Sha } : [],
            replaceable is null ? new HashSet<string>() : new HashSet<string> { replaceable },
            []);
}
