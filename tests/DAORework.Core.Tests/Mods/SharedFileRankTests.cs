using DAORework.Core.Mods.Conflicts;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class SharedFileRankTests
{
    [Theory]
    [InlineData("alistair_main.dlg", SharedFileImpact.CouldBreakQuests)]
    [InlineData("ABI_base.gda", SharedFileImpact.CouldBreakQuests)]
    [InlineData("rules_core.ncs", SharedFileImpact.CouldBreakQuests)]
    [InlineData("hair.DDS", SharedFileImpact.Looks)]
    [InlineData("head.mmh", SharedFileImpact.Looks)]
    [InlineData("mymod_en-us.tlk", SharedFileImpact.Other)]
    [InlineData("pack.erf", SharedFileImpact.Other)]
    public void Of_GoesByTheKindOfFile(string fileName, SharedFileImpact expected) => Assert.Equal(expected, SharedFileRank.Of(fileName));

    [Fact]
    public void Summary_CountsWhatMatters_MostSeriousFirst()
    {
        var summary = SharedFileRank.Summary(["a.dds", "b.dds", "c.ncs", "d.gda", "e.tlk"]);

        Assert.Equal("2 could break quests, 2 only change how things look and 1 is something else.", summary);
    }

    [Fact]
    public void Summary_OneLook_ReadsAsAPersonWouldSayIt() =>
        Assert.Equal("1 only changes how things look.", SharedFileRank.Summary(["a.dds"]));

    [Fact]
    public void Summary_NoFiles_SaysNothing() => Assert.Null(SharedFileRank.Summary([]));

    [Theory]
    [InlineData("alistair_main.dlg", "Alistair's conversation")]
    [InlineData("MORRIGAN_romance.cnv", "Morrigan's conversation")]
    [InlineData("leliana_head.dds", "Leliana's look")]
    [InlineData("gen00fl_dog.dlg", null)]
    [InlineData("alistair_table.gda", null)]
    public void PlainName_SaysOnlyWhatTheNameMakesPlain(string fileName, string? expected) => Assert.Equal(expected, SharedFileNames.For(fileName));

    [Theory]
    [InlineData("party_camp.are")]
    [InlineData("merchant.utc")]
    [InlineData("zevran_kiss.cut")]
    public void Meaning_EveryKindThatCouldBreakQuests_SaysWhatToDo(string fileName)
    {
        Assert.Equal(SharedFileImpact.CouldBreakQuests, SharedFileRank.Of(fileName));
        Assert.Contains("compatibility patch", SharedFileMeaning.For(fileName), StringComparison.Ordinal);
    }

    [Fact]
    public void Meaning_AConversation_NamesWhoseItIs() =>
        Assert.StartsWith("It is Alistair's conversation, so ", SharedFileMeaning.For("alistair_main.dlg"), StringComparison.Ordinal);
}
