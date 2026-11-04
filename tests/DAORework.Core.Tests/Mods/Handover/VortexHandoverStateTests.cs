using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Handover;
using Xunit;

namespace DAORework.Core.Tests.Mods.Handover;

/// <summary>The handover's step, worked out from the game folder (ADR-0028 §3).</summary>
public sealed class VortexHandoverStateTests
{
    private const string Hair = "packages/core/override/hair.dds";
    private const string Mod = "Anto Hairstyles 2-3480-1-2-3";

    private static readonly VortexHandover _handover = new([new VortexCopy(Mod, "anto-1")], [Hair]);

    [Fact]
    public void From_NoRecordAndNoHandover_HasNothingToDo()
    {
        Assert.Equal(VortexHandoverStep.NothingToDo, VortexHandoverState.From(Snapshot(placed: false, live: false), null, []).Step);
    }

    [Fact]
    public void From_ALiveModWithNoCopy_IsTheCopyStep_AndNamesIt()
    {
        var state = VortexHandoverState.From(Snapshot(placed: true, live: true), null, []);

        Assert.Equal(VortexHandoverStep.Copy, state.Step);
        Assert.Equal(new VortexManagedMod(Mod, "Anto Hairstyles 2", 1), Assert.Single(state.Managed));
    }

    [Fact]
    public void From_ALeftBehindMod_IsNotManaged()
    {
        Assert.Equal(VortexHandoverStep.NothingToDo, VortexHandoverState.From(Snapshot(placed: true, live: false), null, []).Step);
    }

    [Fact]
    public void From_CopiedButVortexStillLists_IsThePurgeStep()
    {
        Assert.Equal(VortexHandoverStep.Purge, VortexHandoverState.From(Snapshot(placed: true, live: true), _handover, []).Step);
    }

    [Fact]
    public void From_TheRecordIsEmptyButAPlacedFileIsLeft_IsStillThePurgeStep()
    {
        var snapshot = Snapshot(placed: true, live: true) with { VortexRecords = [new VortexRecord([])] };

        Assert.Equal(VortexHandoverStep.Purge, VortexHandoverState.From(snapshot, _handover, []).Step);
    }

    [Fact]
    public void From_ARecordListingOnlyWhatVortexLost_DoesNotHoldThePurge()
    {
        var lost = new VortexRecord([new VortexPlacedFile("packages/core/override/gone.dds", "Gone-1-1")]) { AtItsTarget = true };
        var snapshot = Snapshot(placed: false, live: false) with { VortexRecords = [lost] };

        Assert.Equal(VortexHandoverStep.TurnOn, VortexHandoverState.From(snapshot, _handover, []).Step);
    }

    [Fact]
    public void From_VortexManagesModsWhileACopyIsOn_IsPlacingAgain()
    {
        Assert.True(VortexHandoverState.From(Snapshot(placed: true, live: true), _handover, ["anto-1"]).PlacingAgain);
        Assert.False(VortexHandoverState.From(Snapshot(placed: true, live: true), _handover, []).PlacingAgain);
    }

    [Fact]
    public void From_ThePurgeIsDone_IsTheTurnOnStep_UntilEveryCopyIsOn()
    {
        var purged = Snapshot(placed: false, live: false) with { VortexRecords = [] };

        Assert.Equal(VortexHandoverStep.TurnOn, VortexHandoverState.From(purged, _handover, []).Step);
        Assert.Equal(VortexHandoverStep.Done, VortexHandoverState.From(purged, _handover, ["ANTO-1"]).Step);
    }

    private static HandInstalledSnapshot Snapshot(bool placed, bool live)
    {
        var record = new VortexRecord([new VortexPlacedFile(Hair, Mod)])
        {
            AtItsTarget = live,
            StagedMods = new HashSet<string> { Mod },
        };
        return new HandInstalledSnapshot([], new Dictionary<string, IReadOnlyList<HandInstalledFile>>(), new Dictionary<string, IReadOnlyList<HandInstalledFile>>(), placed ? [new HandInstalledFile(Hair, 1)] : [])
        {
            VortexRecords = placed || live ? [record] : [],
        };
    }
}
