using DAORework.Core.Mods.Handover;
using Xunit;

namespace DAORework.Core.Tests.Mods.Handover;

/// <summary>The copies are arranged so the same files win as in Vortex (ADR-0028 §3).</summary>
public sealed class VortexLoadOrderTests
{
    [Fact]
    public void Arrange_TheModWhoseCopyIsUsed_ComesLast()
    {
        var (order, unsettled) = VortexLoadOrder.Arrange([("B", ["hair.dds"]), ("A", ["hair.dds", "eyes.dds"])], Winners(("hair.dds", "B")));

        Assert.Equal(["A", "B"], order);
        Assert.Empty(unsettled);
    }

    [Fact]
    public void Arrange_ModsWithNothingInCommon_KeepTheirOrder()
    {
        Assert.Equal(["C", "A", "B"], VortexLoadOrder.Arrange([("C", ["c"]), ("A", ["a"]), ("B", ["b"])], Winners(("c", "C"))).Order);
    }

    [Fact]
    public void Arrange_AChain_IsFollowed()
    {
        var (order, _) = VortexLoadOrder.Arrange([("C", ["x", "y"]), ("B", ["x", "y"]), ("A", ["y"])], Winners(("x", "C"), ("y", "A")));

        Assert.True(order.ToList().IndexOf("B") < order.ToList().IndexOf("C"));
        Assert.True(order.ToList().IndexOf("C") < order.ToList().IndexOf("A"));
    }

    [Fact]
    public void Arrange_OrdersThatContradictEachOther_AreNamedAndNothingIsLost()
    {
        var (order, unsettled) = VortexLoadOrder.Arrange([("A", ["x", "y"]), ("B", ["x", "y"]), ("C", ["z"])], Winners(("x", "A"), ("y", "B")));

        Assert.Equal(["C", "A", "B"], order);
        Assert.Equal(["A", "B"], unsettled);
    }

    [Fact]
    public void Arrange_AWinnerThatWasNotCopied_IsIgnored()
    {
        Assert.Equal(["A"], VortexLoadOrder.Arrange([("A", ["x"])], Winners(("x", "Gone"))).Order);
    }

    private static Dictionary<string, string> Winners(params (string File, string Mod)[] winners) =>
        winners.ToDictionary(w => w.File, w => w.Mod);
}
