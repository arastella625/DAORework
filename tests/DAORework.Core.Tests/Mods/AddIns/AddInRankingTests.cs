using DAORework.Core.Mods.AddIns;
using Xunit;

namespace DAORework.Core.Tests.Mods.AddIns;

/// <summary>The three runs on the owner's install on 2026-11-15 (issue #72): red and blue shared a file, red listed first.</summary>
public sealed class AddInRankingTests
{
    [Theory]
    [InlineData(900, 100, "blue")]
    [InlineData(100, 900, "red")]
    [InlineData(100, 100, "red")]
    public void Winner_IsTheLowerPriority_OrOnATieTheOneListedFirst(int red, int blue, string expected)
    {
        var addIns = new[] { AddIn("red", red, 0), AddIn("blue", blue, 1) };

        Assert.Equal(expected, AddInRanking.Winner(addIns).Uid);
    }

    [Fact]
    public void LoadOrder_IsHighestPriorityFirst_TiesInReverseOfTheList_SoTheWinnerIsLast()
    {
        // What the game's log showed on the tie run: Extra_Dog_Slot, then nez_DyeKit, both 100, the later entry first.
        var addIns = new[] { AddIn("nez_DyeKit", 100, 0), AddIn("qwinn", 109, 1), AddIn("Extra_Dog_Slot", 100, 2), AddIn("promo", 1, 3) };

        var order = AddInRanking.LoadOrder(addIns).Select(a => a.Uid);

        Assert.Equal(["qwinn", "Extra_Dog_Slot", "nez_DyeKit", "promo"], order);
        Assert.Equal("promo", AddInRanking.Winner(addIns).Uid);
    }

    internal static ListedAddIn AddIn(string uid, int priority, int position, string module = "Single Player", bool official = false) =>
        new(uid, uid, module, priority, position, official);
}
