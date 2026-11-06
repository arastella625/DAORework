using DAORework.Core.Mods.Adoption;
using Xunit;

namespace DAORework.Core.Tests.Mods.Adoption;

/// <summary>Choosing who wins a file name mods being adopted share (P2.6-21).</summary>
public sealed class AdoptionOrderTests
{
    [Fact]
    public void Swap_PutsTheWinnerInFrontOfTheModItBeat_AndKeepsTheRest()
    {
        Assert.Equal(["a", "d", "b", "c"], AdoptionOrder.Swap(["a", "b", "c", "d"], "d", ["b"]));
    }

    [Fact]
    public void Swap_WithSeveralOthers_LosesToAllOfThem()
    {
        Assert.Equal(["c", "a", "b"], AdoptionOrder.Swap(["a", "b", "c"], "c", ["b", "a"]));
    }

    [Fact]
    public void Swap_ModsThatAreAlreadyOn_CantBeMoved()
    {
        Assert.False(AdoptionOrder.CanSwap(["a"], "a", ["on"]));
        Assert.False(AdoptionOrder.CanSwap(["a"], "on", ["a"]));
        Assert.Equal(["a"], AdoptionOrder.Swap(["a"], "a", ["on"]));
    }
}
