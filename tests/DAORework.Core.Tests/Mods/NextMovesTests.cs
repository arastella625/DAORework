using DAORework.Core.Mods;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>One next step, worked out from where the player is (P2.6-27).</summary>
public sealed class NextMovesTests
{
    [Theory]
    [InlineData(false, true, true, 5, 5, 5, NextMoveKind.SetUpTheGame)]
    [InlineData(true, true, true, 5, 5, 5, NextMoveKind.SettleTheLastChange)]
    [InlineData(true, false, true, 5, 5, 5, NextMoveKind.FinishVortexMove)]
    [InlineData(true, false, false, 5, 5, 5, NextMoveKind.TakeOverWhatIsInTheGame)]
    [InlineData(true, false, false, 0, 0, 0, NextMoveKind.AddMods)]
    [InlineData(true, false, false, 0, 5, 0, NextMoveKind.TurnOnMods)]
    [InlineData(true, false, false, 0, 5, 1, NextMoveKind.Play)]
    public void For_WhatBlocksEverything_ComesFirst(bool profile, bool unfinished, bool vortex, int found, int library, int on, NextMoveKind expected)
    {
        Assert.Equal(expected, NextMoves.For(new NextMoveFacts(profile, unfinished, vortex, found, library, on)).Kind);
    }

    [Fact]
    public void For_OneModInTheGame_ReadsAsOne()
    {
        Assert.Equal("1 mod is in your game", NextMoves.For(new NextMoveFacts(true, false, false, 1, 0, 0)).Title);
    }
}
