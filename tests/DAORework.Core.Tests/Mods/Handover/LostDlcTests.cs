using DAORework.Core.Catalog;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Handover;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods.Handover;

/// <summary>Official DLC the purge took out of <c>AddIns.xml</c> goes back as it was (ADR-0028 §3).</summary>
public sealed class LostDlcTests
{
    private const string Item = "<AddInItem UID=\"DAO_PRC_EP_1\" />";

    private static readonly KnownItemsCatalog _catalog = new(
        KnownItemsCatalog.CurrentSchemaVersion,
        [new KnownItem("dlc-awakening", "Awakening", KnownItemKind.Official, "https://example.com", "x", [new KnownItemFingerprint(FingerprintKind.AddInUid, "DAO_PRC_EP_1")])],
        []);

    [Fact]
    public void Find_OfficialDlcThatIsGone_IsPutBackAsItWas()
    {
        var change = Assert.Single(LostDlc.Find([new KeptAddIn("DAO_PRC_EP_1", Item)], [], _catalog));

        Assert.Equal("Awakening", change.ModId);
        Assert.Equal(Item, change.Item);
        Assert.True(change.Add);
    }

    [Fact]
    public void Find_OfficialDlcStillThere_NeedsNothing()
    {
        Assert.Empty(LostDlc.Find([new KeptAddIn("DAO_PRC_EP_1", Item)], [new RegisteredAddIn("dao_prc_ep_1", "Awakening")], _catalog));
    }

    [Fact]
    public void Find_AModsEntryThatIsGone_IsLeftGone()
    {
        Assert.Empty(LostDlc.Find([new KeptAddIn("Some_Mod", "<AddInItem UID=\"Some_Mod\" />")], [], _catalog));
    }

    [Fact]
    public void Plan_PuttingDlcBack_SaysSo_AndTheRecordDoesNotClaimIt()
    {
        var restored = LostDlc.Find([new KeptAddIn("DAO_PRC_EP_1", Item)], [], _catalog);

        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [], [], new Dictionary<string, string>(), []) { RestoredAddIns = restored }).Value;

        Assert.Equal(restored, plan.AddInsChanges);
        Assert.Contains("Puts Awakening back in AddIns.xml: the purge in Vortex took it out.", ModActivationPlanWarnings.For(plan, id => id));
        Assert.Empty(plan.NextRecord.AddedAddIns);
    }
}
