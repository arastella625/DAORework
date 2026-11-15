using DAORework.Core.Mods.AddIns;
using Xunit;

namespace DAORework.Core.Tests.Mods.AddIns;

public sealed class AddInClashWordsTests
{
    private static readonly ListedAddIn _fixpack = new("qwinn", "Qwinn's Fixpack", "Single Player", 109, 0, false);
    private static readonly ListedAddIn _dog = new("dog", "Extra Dog Slot", "Single Player", 100, 1, false);
    private static readonly ListedAddIn _dyes = new("dyes", "Universal Dye Kit", "core", 100, 2, false);

    [Fact]
    public void Sentence_TwoAddIns_SaysWhoseCopyAndBothPriorities_ThenWhatTheFileIs()
    {
        var sentence = AddInClashWords.Sentence(new AddInClash("party.ncs", _dog, [_fixpack]));

        Assert.Equal(
            "party.ncs is in both Extra Dog Slot and Qwinn's Fixpack. The game uses Extra Dog Slot's copy, because its Priority is lower (100, against 109). It is a script, so what the other mod does with it is lost. Look for a compatibility patch.",
            sentence);
    }

    [Fact]
    public void Sentence_EqualPriorities_SaysItComesFirstInAddInsXml()
    {
        var sentence = AddInClashWords.Sentence(new AddInClash("readme.txt", _dog, [_dyes]));

        Assert.Equal("readme.txt is in both Extra Dog Slot and Universal Dye Kit. The game uses Extra Dog Slot's copy, because they have the same Priority (100) and it comes first in AddIns.xml.", sentence);
    }

    [Fact]
    public void Sentence_ThreeAddIns_ListsThemAndSaysTheLowest()
    {
        var low = _dog with { Priority = 1 };

        var sentence = AddInClashWords.Sentence(new AddInClash("a.bin", low, [_fixpack, _dyes]));

        Assert.StartsWith("a.bin is in Extra Dog Slot, Qwinn's Fixpack and Universal Dye Kit. The game uses Extra Dog Slot's copy, because its Priority is the lowest (1).", sentence, StringComparison.Ordinal);
    }
}
