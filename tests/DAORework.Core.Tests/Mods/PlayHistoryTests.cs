using DAORework.Core.Mods.Troubleshooting;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class PlayHistoryTests
{
    private static readonly DateTimeOffset _tuesday = new(2026, 11, 3, 19, 5, 0, TimeSpan.Zero);

    [Fact]
    public void With_TheSameModsAgain_OnlyMovesTheTimeOn()
    {
        var later = _tuesday.AddDays(1);

        var history = PlayHistory.Empty.With(new PlayRecord(_tuesday, ["a"])).With(new PlayRecord(later, ["a"]));

        Assert.Equal(later, Assert.Single(history.Plays).PlayedAt);
    }

    [Fact]
    public void With_KeepsTheLastTen()
    {
        var history = PlayHistory.Empty;
        for (var i = 0; i < 15; i++)
        {
            history = history.With(new PlayRecord(_tuesday.AddDays(i), [$"mod{i}"]));
        }

        Assert.Equal(10, history.Plays.Count);
        Assert.Equal(["mod14"], history.Plays[^1].ModIds);
    }

    [Fact]
    public void ChangesSince_TheLatestGameHadTheseMods_ComparesWithTheOneBefore()
    {
        var history = PlayHistory.Empty.With(new PlayRecord(_tuesday, ["a", "b"])).With(new PlayRecord(_tuesday.AddDays(2), ["a", "b", "c", "d"]));

        var changes = history.ChangesSince(["a", "b", "c", "d"])!;

        Assert.Equal(_tuesday, changes.Then.PlayedAt);
        Assert.Equal(["c", "d"], changes.Added);
        Assert.Empty(changes.Removed);
        Assert.False(changes.OrderChanged);
    }

    [Fact]
    public void ChangesSince_ModsChangedSinceTheLastGame_ComparesWithIt()
    {
        var changes = PlayHistory.Empty.With(new PlayRecord(_tuesday, ["a", "b", "c"])).ChangesSince(["a", "c", "e"])!;

        Assert.Equal(["e"], changes.Added);
        Assert.Equal(["b"], changes.Removed);
        Assert.False(changes.OrderChanged);
    }

    [Fact]
    public void ChangesSince_TheSameModsInAnotherOrder_SaysSo()
    {
        var changes = PlayHistory.Empty.With(new PlayRecord(_tuesday, ["a", "b"])).ChangesSince(["b", "a"])!;

        Assert.Empty(changes.Added);
        Assert.Empty(changes.Removed);
        Assert.True(changes.OrderChanged);
    }

    [Fact]
    public void ChangesSince_NoGameHadOtherMods_SaysNothing()
    {
        Assert.Null(PlayHistory.Empty.ChangesSince(["a"]));
        Assert.Null(PlayHistory.Empty.With(new PlayRecord(_tuesday, ["a"])).ChangesSince(["A"]));
    }
}
