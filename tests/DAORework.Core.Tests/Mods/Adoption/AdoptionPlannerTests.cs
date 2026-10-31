using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods.Adoption;

public sealed class AdoptionPlannerTests
{
    private const string Override = "packages/core/override/";

    private readonly Dictionary<string, string> _disk = new(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Plan_OverrideFolder_MovesEachFileIntoDaoreworksFolderAndRecordsIt()
    {
        var hair = Adopt(Mod("better-hair-1", ModKind.Override, ("textures/hair.dds", "h"), ("brow.dds", "b")), ("textures/hair.dds", Override + "Better Hair/textures/hair.dds"), ("brow.dds", Override + "Better Hair/brow.dds"));

        var plan = Plan(DeploymentRecord.Empty, hair);

        Assert.Equal(
            [(Override + "Better Hair/brow.dds", Override + "DAORework/better-hair-1/brow.dds"), (Override + "Better Hair/textures/hair.dds", Override + "DAORework/better-hair-1/textures/hair.dds")],
            plan.Operations.Select(o => (o.MovedFrom, o.TargetPath)));
        Assert.All(plan.Operations, o => Assert.Equal(PlanAction.Move, o.Action));
        Assert.Equal("h", plan.Operations[1].ExpectedSha256);
        Assert.Equal(["better-hair-1"], plan.NextRecord.ActiveModIds);
        Assert.Equal(2, plan.NextRecord.Files.Count);
        Assert.Same(DeploymentRecord.Empty, plan.CurrentRecord);
        Assert.False(plan.IsBlocked);
        Assert.Empty(plan.MissingRecordedFiles);
        Assert.Equal(0, plan.BytesToWrite.Bytes);
    }

    [Fact]
    public void Plan_RegisteredAddIn_IsRecordedWhereItIsAndItsOverrideFileMoved()
    {
        _disk["AddIns/Extra_Dog_Slot/core/dog.gda"] = "dog";
        var dog = Adopt(
            Mod("extra-dog-slot-1", ModKind.AddIn, ("Manifest.xml", "m"), ("Contents/addins/Extra_Dog_Slot/core/dog.gda", "dog"), ("Contents/packages/core/override/whistle.uti", "w")) with { AddInUid = "Extra_Dog_Slot" },
            ("Contents/addins/Extra_Dog_Slot/core/dog.gda", "AddIns/Extra_Dog_Slot/core/dog.gda"),
            ("Contents/packages/core/override/whistle.uti", Override + "whistle.uti"));

        var plan = Plan(DeploymentRecord.Empty, dog, registered: "Extra_Dog_Slot");

        var move = Assert.Single(plan.Operations);
        Assert.Equal((PlanAction.Move, Override + "whistle.uti"), (move.Action, move.MovedFrom));
        Assert.Equal(1, plan.AdoptedInPlaceCount);
        Assert.Equal(0, plan.UnchangedFileCount);
        Assert.Empty(plan.AddInsChanges);
        Assert.False(plan.IsBlocked);
        Assert.Equal([new RecordedAddIn("Extra_Dog_Slot", "extra-dog-slot-1")], plan.NextRecord.AddedAddIns);
        Assert.Contains("  Adopt     1 file where they are (recorded, not changed)", ModActivationPlanText.Render(plan));
    }

    /// <summary>Adopting an add-in that is all in place moves nothing, but it still changes the record, so it is applied.</summary>
    [Fact]
    public void Plan_AddInAllInPlace_IsNotEmpty()
    {
        _disk["AddIns/Mod/a.erf"] = "a";
        var mod = Adopt(Mod("mod-1", ModKind.AddIn, ("Manifest.xml", "m"), ("Contents/addins/Mod/a.erf", "a")) with { AddInUid = "Mod" }, ("Contents/addins/Mod/a.erf", "AddIns/Mod/a.erf"));

        var plan = Plan(DeploymentRecord.Empty, mod, registered: "Mod");

        Assert.Empty(plan.Operations);
        Assert.False(plan.IsEmpty);
    }

    [Fact]
    public void Plan_TwoAdoptedFoldersWithTheSameFileName_LaterWinsAndTheEarlierCopyIsDeletedWhereItIs()
    {
        var first = Adopt(Mod("first-1", ModKind.Override, ("hair.dds", "one")), ("hair.dds", Override + "First/hair.dds"));
        var second = Adopt(Mod("second-1", ModKind.Override, ("hair.dds", "two")), ("hair.dds", Override + "Second/hair.dds"));

        var plan = Plan(DeploymentRecord.Empty, first, second);

        Assert.Equal(
            [(PlanAction.Delete, Override + "First/hair.dds", "one"), (PlanAction.Move, Override + "DAORework/second-1/hair.dds", "two")],
            plan.Operations.Select(o => (o.Action, o.TargetPath, o.ExpectedSha256)));
        Assert.Equal("second-1", Assert.Single(plan.Conflicts.OverrideConflicts).Winner.ModId);
        Assert.Equal([Override + "DAORework/second-1/hair.dds"], plan.NextRecord.Files.Select(f => f.TargetPath));
    }

    [Fact]
    public void Plan_FolderWithAProgram_LeavesTheProgramWhereItIs()
    {
        var mod = Adopt(Mod("tools-1", ModKind.Override, ("a.dds", "a"), ("setup.exe", "x")), ("a.dds", Override + "Tools/a.dds"), ("setup.exe", Override + "Tools/setup.exe"));

        var plan = Plan(DeploymentRecord.Empty, mod);

        var move = Assert.Single(plan.Operations);
        Assert.Equal(Override + "Tools/a.dds", move.MovedFrom);
        Assert.DoesNotContain(plan.Operations, o => o.TargetPath.EndsWith("setup.exe", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.NextRecord.Files, f => f.TargetPath.EndsWith("setup.exe", StringComparison.Ordinal));
    }

    [Fact]
    public void Plan_ModAlreadyActive_FailsWithItsCode()
    {
        var mod = Mod("hair-1", ModKind.Override, ("a.dds", "a"));
        var input = new PlanInput(new DeploymentRecord(["hair-1"], [], []), [mod], [], _disk, []);

        var result = AdoptionPlanner.Plan(input, [Adopt(mod, ("a.dds", Override + "Hair/a.dds"))]);

        Assert.Equal(DiagnosticCodes.ModAdoptAlreadyActive, result.Error!.Code);
    }

    [Fact]
    public void Plan_LibraryFileNotOnDisk_FailsWithItsCode()
    {
        var mod = Adopt(Mod("hair-1", ModKind.Override, ("a.dds", "a"), ("b.dds", "b")), ("a.dds", Override + "Hair/a.dds"));

        var result = AdoptionPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [], [], _disk, []), [mod]);

        Assert.Equal(DiagnosticCodes.ModAdoptSourceMissing, result.Error!.Code);
    }

    private static LibraryMod Mod(string id, ModKind kind, params (string Path, string Sha)[] files) =>
        new(id, id, kind, files.Select(f => new LibraryModFile(f.Path, 10, f.Sha)).ToList());

    private static AdoptedMod Adopt(LibraryMod mod, params (string LibraryPath, string DiskPath)[] sources) =>
        new(mod, sources.ToDictionary(s => s.LibraryPath, s => s.DiskPath));

    private ModActivationPlan Plan(DeploymentRecord current, params AdoptedMod[] adopted) => Plan(current, adopted[0], adopted[1..], registered: null);

    private ModActivationPlan Plan(DeploymentRecord current, AdoptedMod first, string registered) => Plan(current, first, [], registered);

    private ModActivationPlan Plan(DeploymentRecord current, AdoptedMod first, AdoptedMod[] more, string? registered) =>
        AdoptionPlanner.Plan(new PlanInput(current, [], [], _disk, registered is null ? [] : [registered]), [first, .. more]).Value;
}
