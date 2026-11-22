using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>Home's one headline, sentence and button (P2.10-02, ADR-0050 §1).</summary>
public sealed class HomeHeadlinesTests
{
    private static readonly GameCheck Clean = new(0, false);

    public static TheoryData<NextMoveKind, GameCheck?, HomeHeadlineKind> Order => new()
    {
        { NextMoveKind.SetUpTheGame, new GameCheck(3, false), HomeHeadlineKind.Welcome },
        { NextMoveKind.SettleTheLastChange, new GameCheck(3, false), HomeHeadlineKind.OneThingFirst },
        { NextMoveKind.TakeOverWhatIsInTheGame, new GameCheck(1, false), HomeHeadlineKind.NotReady },
        { NextMoveKind.Play, new GameCheck(1, true), HomeHeadlineKind.NotReady },
        { NextMoveKind.TakeOverWhatIsInTheGame, null, HomeHeadlineKind.OneThingFirst },
        { NextMoveKind.TurnOnMods, Clean, HomeHeadlineKind.OneThingFirst },
        { NextMoveKind.Play, null, HomeHeadlineKind.Checking },
        { NextMoveKind.Play, new GameCheck(0, true), HomeHeadlineKind.ReadyWithoutMods },
        { NextMoveKind.Play, Clean, HomeHeadlineKind.Ready },
    };

    [Theory]
    [MemberData(nameof(Order))]
    public void For_WhatBlocksEverything_ComesFirst_ThenWhatStopsTheGame_ThenTheNextStep(NextMoveKind move, GameCheck? check, HomeHeadlineKind expected)
    {
        Assert.Equal(expected, HomeHeadlines.For(Move(move), check).Kind);
    }

    [Fact]
    public void For_OneThingFirst_SaysTheStep_AndKeepsItsButton()
    {
        var headline = HomeHeadlines.For(Move(NextMoveKind.TakeOverWhatIsInTheGame), Clean);

        Assert.Equal("One thing first", headline.Title);
        Assert.Equal("3 mods are in your game.", headline.Sentence);
        Assert.Equal("Take them over", headline.Button);
        Assert.Equal(HomeButton.TakeNextMove, headline.Action);
    }

    [Fact]
    public void For_AStepWithNoButtonOnMods_StillHasOneOnHome()
    {
        Assert.Equal("Turn them on", HomeHeadlines.For(Move(NextMoveKind.TurnOnMods), Clean).Button);
        Assert.Equal("Go to Mods", HomeHeadlines.For(Move(NextMoveKind.FinishVortexMove), Clean).Button);
    }

    [Fact]
    public void For_EveryHeadlineButChecking_HasOneButton()
    {
        foreach (var row in Order)
        {
            var headline = HomeHeadlines.For(Move((NextMoveKind)row[0]), (GameCheck?)row[1]);
            Assert.Equal(headline.Kind == HomeHeadlineKind.Checking, headline.Button is null);
        }
    }

    [Fact]
    public void For_Problems_AreCountedAsAPersonWould()
    {
        Assert.Equal("1 problem to fix before the game can start.", HomeHeadlines.For(Move(NextMoveKind.Play), new GameCheck(1, false)).Sentence);
        Assert.Equal("2 problems to fix before the game can start.", HomeHeadlines.For(Move(NextMoveKind.Play), new GameCheck(2, false)).Sentence);
    }

    [Fact]
    public void From_CountsErrorsOnly_AndNoticesTheDocumentsFolder()
    {
        var check = GameCheck.From(
        [
            new DiagnosticIssue(DiagnosticSeverity.Error, "DAO_X", "Stops it"),
            new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.DocumentsNotTheGames, "Elsewhere"),
            new DiagnosticIssue(DiagnosticSeverity.Info, "DAO_Y", "Fine"),
        ]);

        Assert.Equal(new GameCheck(1, true), check);
    }

    // The moves' own words, as NextMoves says them for a player with 3 mods in the game and 5 in the Library.
    private static NextMove Move(NextMoveKind kind) => kind switch
    {
        NextMoveKind.SetUpTheGame => NextMoves.For(new NextMoveFacts(false, false, false, 0, 0, 0)),
        NextMoveKind.SettleTheLastChange => NextMoves.For(new NextMoveFacts(true, true, false, 0, 0, 0)),
        NextMoveKind.FinishVortexMove => NextMoves.For(new NextMoveFacts(true, false, true, 0, 0, 0)),
        NextMoveKind.TakeOverWhatIsInTheGame => NextMoves.For(new NextMoveFacts(true, false, false, 3, 5, 0)),
        NextMoveKind.TurnOnMods => NextMoves.For(new NextMoveFacts(true, false, false, 0, 5, 0)),
        _ => NextMoves.For(new NextMoveFacts(true, false, false, 0, 5, 1)),
    };
}
