using DAORework.Core.Mods.Troubleshooting;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ProblemSearchTests
{
    private static readonly string[] _mods = [.. Enumerable.Range(0, 137).Select(i => $"mod{i}")];

    [Fact]
    public void Answer_WithOneCauseAmong137_FindsEachOneWithinNineRounds()
    {
        foreach (var culprit in _mods)
        {
            var (found, rounds) = Run(ProblemSearch.Start(_mods), culprit);

            Assert.Equal(ProblemSearchEnd.Found, found.End);
            Assert.Equal(culprit, found.Culprit);
            Assert.InRange(rounds, 1, 9);
        }
    }

    [Fact]
    public void Answer_TheProblemNeverGoes_EndsOnNotOneOfYourMods_AfterOneMoreRound()
    {
        var (found, rounds) = Run(ProblemSearch.Start(_mods), culprit: null);

        Assert.Equal(ProblemSearchEnd.NotFound, found.End);
        Assert.Null(found.Culprit);
        Assert.InRange(rounds, 8, 9);
    }

    [Fact]
    public void Start_TurnsOffHalf_AndKeepsThePlayersOrder()
    {
        var search = ProblemSearch.Start(["a", "b", "c", "d", "e"]);

        Assert.Equal(["d", "e"], search.Off);
        Assert.Equal(["a", "b", "c"], search.Keeping());
    }

    [Fact]
    public void Answer_EveryRound_KeepsAllButTheHalfBeingTested_SoAModsNeedsStayOn()
    {
        var next = ProblemSearch.Start(["a", "b", "c", "d"]).Answer(stillHappens: true);

        Assert.Equal(["a", "b"], next.Suspects);
        Assert.Equal(["a", "c", "d"], next.Keeping());
    }

    [Fact]
    public void Start_OneMod_TestsThatMod()
    {
        var search = ProblemSearch.Start(["only"]);

        Assert.Empty(search.Keeping());
        Assert.Equal("only", search.Answer(stillHappens: false).Culprit);
        Assert.Equal(ProblemSearchEnd.NotFound, search.Answer(stillHappens: true).End);
    }

    [Fact]
    public void RoundsLeft_StartsAtSevenMoreFor137() => Assert.Equal(7, ProblemSearch.Start(_mods).RoundsLeft());

    private static (ProblemSearch Search, int Rounds) Run(ProblemSearch search, string? culprit)
    {
        var rounds = 0;
        while (search.End == ProblemSearchEnd.None)
        {
            rounds++;
            search = search.Answer(stillHappens: culprit is null || search.Keeping().Contains(culprit));
        }

        return (search, rounds);
    }
}
