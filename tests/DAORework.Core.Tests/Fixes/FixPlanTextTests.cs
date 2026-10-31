using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Fixes;

public sealed class FixPlanTextTests
{
    private const string Exe = "bin_ship/daorigins.exe";

    private static readonly FixEntry _laa = new(
        "large-address-aware-aaaaaaaa",
        FixIds.LargeAddressAware,
        null,
        [new FixFile(Exe, new LibraryModFile("patched/daorigins.exe", 2048, "patched"))],
        [new LibraryModFile("original/daorigins.exe", 2048, "original")]);

    private static readonly DeploymentRecord _applied = new([_laa.LibraryId], [new RecordedFile(Exe, "patched", _laa.LibraryId) { BeforeSha256 = "original" }], []);

    [Fact]
    public void Apply_NamesTheFixEachFileAndTheBackup()
    {
        var lines = FixPlanText.Render(FixPlanner.PlanApply(_laa, State(DeploymentRecord.Empty, "original", replaceable: true)), "4 GB patch");

        Assert.Equal(
            ["Apply: 4 GB patch (large-address-aware-aaaaaaaa)", "  Replace bin_ship/daorigins.exe", "  Writes 2 KB; the files it replaces are backed up first."],
            lines);
    }

    [Fact]
    public void Remove_PutsTheOriginalBack()
    {
        var lines = FixPlanText.Render(FixPlanner.PlanRemove(_laa.LibraryId, State(_applied, "patched"), [_laa]), "4 GB patch");

        Assert.Equal("Remove: 4 GB patch (large-address-aware-aaaaaaaa)", lines[0]);
        Assert.Equal("  Put back the original bin_ship/daorigins.exe", lines[1]);
    }

    [Fact]
    public void Remove_AfterAnOutsideChange_SaysSoAndThatNothingChanges()
    {
        var lines = FixPlanText.Render(FixPlanner.PlanRemove(_laa.LibraryId, State(_applied, "original"), [_laa]), "4 GB patch");

        Assert.Contains("Nothing to change: the install folder already matches.", lines);
        Assert.Contains("  Changed outside DAORework since, so left in place: bin_ship/daorigins.exe", lines);
    }

    [Fact]
    public void AlreadyApplied_SaysSo()
    {
        var lines = FixPlanText.Render(FixPlanner.PlanApply(_laa, State(_applied, "patched")), "4 GB patch");

        Assert.Contains("  Already in place: 1 file(s)", lines);
    }

    [Fact]
    public void Blocked_ListsWhyAndSaysNothingChanges()
    {
        var state = State(DeploymentRecord.Empty, "original", replaceable: true) with { Blockers = [new PlanBlocker(PlanBlockerKind.FileInUse, Exe, "daorigins.exe is in use; close the game.")] };

        var lines = FixPlanText.Render(FixPlanner.PlanApply(_laa, state), "4 GB patch");

        Assert.Equal(["Blocked; nothing will be changed:", "  x daorigins.exe is in use; close the game."], lines.TakeLast(2));
    }

    [Fact]
    public void BlockedBeforeAnythingCouldBePlanned_SaysSo()
    {
        var lines = FixPlanText.Render(FixPlanner.PlanApply(_laa, State(DeploymentRecord.Empty, "someone-elses")), "4 GB patch");

        Assert.Equal("No changes can be planned until the problems below are resolved.", lines[1]);
    }

    [Fact]
    public void MissingRecordedFile_IsListed()
    {
        var lines = FixPlanText.Render(FixPlanner.PlanRemove(_laa.LibraryId, new InstallState(_applied, new Dictionary<string, string>(), new HashSet<string>(), []), [_laa]), "4 GB patch");

        Assert.Contains("  Missing since DAORework placed it: bin_ship/daorigins.exe", lines);
    }

    [Fact]
    public void AFileWhereNothingWas_IsAddedAndOnRemovalDeleted()
    {
        var dxvk = new FixEntry("dxvk-2-3-cccccccc", FixIds.Dxvk, "2.3", [new FixFile("bin_ship/d3d9.dll", new LibraryModFile("d3d9.dll", 5, "dxvk23"))], []);
        var none = new InstallState(DeploymentRecord.Empty, new Dictionary<string, string>(), new HashSet<string>(), []);
        var applied = FixPlanner.PlanApply(dxvk, none);
        var present = new InstallState(applied.Change.NextRecord, new Dictionary<string, string> { ["bin_ship/d3d9.dll"] = "dxvk23" }, new HashSet<string>(), []);

        Assert.Equal("  Add bin_ship/d3d9.dll", FixPlanText.Render(applied, "DXVK 2.3")[1]);
        Assert.Equal("  Delete bin_ship/d3d9.dll", FixPlanText.Render(FixPlanner.PlanRemove(dxvk.LibraryId, present, [dxvk]), "DXVK 2.3")[1]);
    }

    private static InstallState State(DeploymentRecord record, string exeSha, bool replaceable = false) =>
        new(record, new Dictionary<string, string> { [Exe] = exeSha }, replaceable ? new HashSet<string> { exeSha } : new HashSet<string>(), []);
}
