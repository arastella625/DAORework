using DAORework.Core.Mods;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>The way to a first game with mods (P2.6-36).</summary>
public sealed class FirstStepsTests
{
    [Fact]
    public void For_ANewPlayer_HasNothingDone()
    {
        Assert.All(FirstSteps.For(new NextMoveFacts(false, false, false, 0, 0, 0)), s => Assert.False(s.IsDone));
    }

    [Fact]
    public void For_ModsFoundInTheGame_CountAsAdded()
    {
        Assert.Equal([true, true, false], FirstSteps.For(new NextMoveFacts(true, false, false, 4, 0, 0)).Select(s => s.IsDone));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void AreNeeded_OnlyUntilAModIsOn(int on, bool expected)
    {
        Assert.Equal(expected, FirstSteps.AreNeeded(new NextMoveFacts(true, false, false, 0, 1, on)));
    }
}
