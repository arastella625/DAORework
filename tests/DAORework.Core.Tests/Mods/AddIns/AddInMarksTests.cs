using DAORework.Core.Mods.AddIns;
using Xunit;
using static DAORework.Core.Tests.Mods.AddIns.AddInRankingTests;

namespace DAORework.Core.Tests.Mods.AddIns;

public sealed class AddInMarksTests
{
    private static readonly ListedAddIn _fixpack = AddIn("qwinn", 109, 0);
    private static readonly ListedAddIn _dog = AddIn("Extra_Dog_Slot", 100, 1);
    private static readonly ListedAddIn _dyes = AddIn("nez_DyeKit", 100, 2);

    [Fact]
    public void For_EachAddIn_SaysItsPriority_AndWhetherItsCopyIsUsed()
    {
        var report = new AddInsReport([_fixpack, _dog, _dyes], [new AddInClash("party.ncs", _dog, [_fixpack])], []);

        var marks = AddInMarks.For(report);

        Assert.Equal("Priority 109 · loses", marks["QWINN"].Text);
        Assert.Equal("Priority 100 · wins", marks["extra_dog_slot"].Text);
        Assert.Equal("Priority 100", marks["nez_DyeKit"].Text);
        Assert.False(marks["nez_DyeKit"].Shares);
        Assert.Empty(marks["nez_DyeKit"].Sentences);
        Assert.Equal([AddInClashWords.Sentence(report.Clashes[0]), AddInClashWords.Advice], marks["qwinn"].Sentences);
    }

    [Fact]
    public void For_AnAddInThatWinsOneAndLosesAnother_SaysBoth()
    {
        var clashes = new[] { new AddInClash("a.dds", _dog, [_fixpack]), new AddInClash("b.dds", _fixpack, [_dog]) };

        var mark = AddInMarks.For(new AddInsReport([_fixpack, _dog], clashes, []))["qwinn"];

        Assert.Equal("Priority 109 · wins and loses", mark.Text);
        Assert.True(mark.Shares);
    }

    [Fact]
    public void For_ManySharedFiles_NamesTheFirstFew_AndCountsTheRest()
    {
        var clashes = Enumerable.Range(0, AddInMarks.SentencesShown + 2).Select(i => new AddInClash($"f{i}.dds", _dog, [_fixpack])).ToList();

        var sentences = AddInMarks.For(new AddInsReport([_fixpack, _dog], clashes, []))["qwinn"].Sentences;

        Assert.Equal(AddInMarks.SentencesShown + 2, sentences.Count);
        Assert.Equal("2 more files are in it and another add-in.", sentences[^2]);
        Assert.Equal(AddInClashWords.Advice, sentences[^1]);
    }
}
