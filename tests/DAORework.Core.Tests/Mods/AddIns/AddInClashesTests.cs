using DAORework.Core.Mods.AddIns;
using Xunit;
using static DAORework.Core.Tests.Mods.AddIns.AddInRankingTests;

namespace DAORework.Core.Tests.Mods.AddIns;

public sealed class AddInClashesTests
{
    [Fact]
    public void Find_ACoreFileTwoAddInsHave_NamesTheLowerPriorityAsTheWinner()
    {
        var fixpack = AddIn("fixpack", 109, 0);
        var dog = AddIn("dog", 100, 1);

        var clash = Assert.Single(AddInClashes.Find([Contents(fixpack, core: ["Party_Base.NCS", "fix.dlg"]), Contents(dog, core: ["party_base.ncs"])]));

        Assert.Equal("Party_Base.NCS", clash.FileName);
        Assert.Equal(dog, clash.Winner);
        Assert.Equal([fixpack], clash.Others);
        Assert.False(clash.WonOnPosition);
    }

    [Fact]
    public void Find_ModuleFiles_ClashOnlyWithinOneCampaign()
    {
        var origins = Contents(AddIn("a", 100, 0), module: ["den_area.are"]);
        var awakening = Contents(AddIn("b", 100, 1, module: "DAO_PRC_EP_1"), module: ["den_area.are"]);
        var alsoOrigins = Contents(AddIn("c", 50, 2), module: ["DEN_AREA.ARE"]);

        Assert.Empty(AddInClashes.Find([origins, awakening]));
        Assert.Equal("c", Assert.Single(AddInClashes.Find([origins, awakening, alsoOrigins])).Winner.Uid);
    }

    [Fact]
    public void Find_LeavesOutArchiveManifests_NamesOnlyBioWareShares_AndOneAddInsOwnRepeats()
    {
        var official = Contents(AddIn("dlc1", 500, 0, official: true), core: ["shared.gda"]);
        var otherOfficial = Contents(AddIn("dlc2", 501, 1, official: true), core: ["shared.gda"]);
        var mod = Contents(AddIn("mod", 100, 2), core: ["texture.dds", "texture.dds"], module: ["manifest.xml"]);
        var other = Contents(AddIn("other", 100, 3), module: ["manifest.xml"]);

        Assert.Empty(AddInClashes.Find([official, otherOfficial, mod, other]));
    }

    [Fact]
    public void Find_AModAndADlc_IsReported_AndQuestFilesComeFirst()
    {
        var dlc = Contents(AddIn("dlc", 500, 0, official: true), core: ["armor.dds", "plot.plo"]);
        var mod = Contents(AddIn("mod", 100, 1), core: ["armor.dds", "plot.plo"]);

        var clashes = AddInClashes.Find([dlc, mod]);

        Assert.Equal(["plot.plo", "armor.dds"], clashes.Select(c => c.FileName));
        Assert.All(clashes, c => Assert.Equal("mod", c.Winner.Uid));
    }

    [Fact]
    public void Find_AFileInBothCoreAndModule_IsSaidOnce()
    {
        var a = Contents(AddIn("a", 100, 0), core: ["x.ncs"], module: ["x.ncs"]);
        var b = Contents(AddIn("b", 100, 1), core: ["x.ncs"], module: ["x.ncs"]);

        var clash = Assert.Single(AddInClashes.Find([a, b]));

        Assert.True(clash.WonOnPosition);
        Assert.Equal("a", clash.Winner.Uid);
    }

    private static AddInContents Contents(ListedAddIn addIn, string[]? core = null, string[]? module = null) => new(addIn, core ?? [], module ?? []);
}
