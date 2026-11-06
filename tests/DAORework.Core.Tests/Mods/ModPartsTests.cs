using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>A mod that is only part of another counts as a copy of it (ADR-0039).</summary>
public sealed class ModPartsTests
{
    private static readonly DateTimeOffset _day = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Find_APartWithTheSameName_IsACopyOfTheWhole_EvenIfNeitherIsOn()
    {
        var whole = Entry("whole", "14 New Face Tattoos", 0, ("a.dds", "1"), ("b.dds", "2"));
        var part = Entry("part", "14 New Face Tattoos", 1, ("a.dds", "1"));

        var copies = ModCopies.Find([part, whole]);

        Assert.Equal(copies["whole"].Group, copies["part"].Group);
        Assert.Equal(2, copies["part"].Count);
        Assert.True(copies["part"].IsPart);
        Assert.False(copies["part"].HoldsAll);
        Assert.False(copies["whole"].IsPart);
        Assert.True(copies["whole"].HoldsAll);
    }

    [Fact]
    public void Find_TheWhole_ComesFirst_ThoughThePartIsNewer()
    {
        var whole = Entry("whole", "Eyes", 0, ("a.dds", "1"), ("b.dds", "2"));
        var part = Entry("part", "Eyes", 5, ("a.dds", "1"));

        var copies = ModCopies.Find([part, whole]);

        Assert.Equal(0, copies["whole"].Rank);
        Assert.Equal(1, copies["part"].Rank);
    }

    [Theory]
    [InlineData("Additional Hairs for DA (Part 1)", "Additional Hairs for DA Part 1")]
    [InlineData("additional_hairs--for da", "ADDITIONAL HAIRS FOR DA")]
    public void Find_NamesMatch_WithPunctuationAndCaseIgnored(string first, string second)
    {
        var copies = ModCopies.Find([Entry("whole", first, 0, ("a.dds", "1"), ("b.dds", "2")), Entry("part", second, 1, ("a.dds", "1"))]);

        Assert.True(copies["part"].IsPart);
    }

    [Fact]
    public void Find_AnotherName_IsNoPart_WhileTheBiggerModIsOff()
    {
        var entries = new[] { Entry("big", "Big Pack", 0, ("a.dds", "1"), ("b.dds", "2")), Entry("small", "Blue Eyes", 1, ("a.dds", "1")) };

        Assert.Empty(ModCopies.Find(entries));
        Assert.Empty(ModCopies.Find(entries, ["small"]));
    }

    [Fact]
    public void Find_AnotherName_IsAPart_WhenTheBiggerModIsOn_SinceItAddsNothing()
    {
        var copies = ModCopies.Find([Entry("big", "Big Pack", 0, ("a.dds", "1"), ("b.dds", "2")), Entry("small", "Blue Eyes", 1, ("a.dds", "1"))], ["BIG"]);

        Assert.True(copies["small"].IsPart);
    }

    [Fact]
    public void Find_TheSameName_WithDifferentContents_IsNoCopy()
    {
        var copies = ModCopies.Find([Entry("one", "Hair", 0, ("a.dds", "1"), ("b.dds", "2")), Entry("two", "Hair", 1, ("a.dds", "9"))]);

        Assert.Empty(copies);
    }

    [Fact]
    public void Find_TheSameHash_UnderAnotherFileName_IsNoPart()
    {
        var copies = ModCopies.Find([Entry("one", "Hair", 0, ("a.dds", "1"), ("b.dds", "2")), Entry("two", "Hair", 1, ("c.dds", "1"))]);

        Assert.Empty(copies);
    }

    [Fact]
    public void Find_PartsOfParts_EndInOneSet_UnderTheBiggest()
    {
        var biggest = Entry("biggest", "Hair", 0, ("a.dds", "1"), ("b.dds", "2"), ("c.dds", "3"));
        var middle = Entry("middle", "Hair", 1, ("a.dds", "1"), ("b.dds", "2"));
        var smallest = Entry("smallest", "Hair", 2, ("a.dds", "1"));

        var copies = ModCopies.Find([smallest, middle, biggest]);

        Assert.Single(copies.Values.Select(c => c.Group).Distinct());
        Assert.Equal(3, copies["smallest"].Count);
        Assert.Equal(0, copies["biggest"].Rank);
    }

    [Fact]
    public void Find_AnIdenticalPairAndAPart_AreOneSet()
    {
        var copies = ModCopies.Find([
            Entry("whole-1", "Hair", 0, ("a.dds", "1"), ("b.dds", "2")),
            Entry("whole-2", "Hair", 1, ("a.dds", "1"), ("b.dds", "2")),
            Entry("part", "Hair", 2, ("a.dds", "1"))]);

        Assert.Equal(3, copies["part"].Count);
        Assert.True(copies["whole-1"].HoldsAll);
        Assert.True(copies["whole-2"].HoldsAll);
    }

    [Fact]
    public void Find_APartOfTwoMods_JoinsTheOneOfItsOwnName()
    {
        var copies = ModCopies.Find(
            [Entry("other", "Big Pack", 0, ("a.dds", "1"), ("b.dds", "2"), ("c.dds", "3")), Entry("own", "Hair", 1, ("a.dds", "1"), ("d.dds", "4")), Entry("part", "Hair", 2, ("a.dds", "1"))],
            ["other"]);

        Assert.Equal(copies["own"].Group, copies["part"].Group);
        Assert.False(copies.ContainsKey("other") && copies["other"].Group == copies["part"].Group);
    }

    [Fact]
    public void Find_AnAddInsFiles_AreNotAWholeForAnOverrideMod()
    {
        var addIn = new LibraryEntry(new LibraryMod("addin", "Hair", ModKind.AddIn, [new LibraryModFile("Contents/a.dds", 1, "1"), new LibraryModFile("Contents/b.dds", 1, "2")], "hair_uid"), "s", "sha", _day);

        Assert.Empty(ModCopies.Find([addIn, Entry("part", "Hair", 1, ("a.dds", "1"))], ["addin"]));
    }

    [Fact]
    public void Find_AddInCopiesWhereNoneHoldsEverything_AreNotParts()
    {
        var one = new LibraryEntry(new LibraryMod("one", "Dog", ModKind.AddIn, [new LibraryModFile("a.gda", 1, "1")], "dog"), "s", "sha", _day);
        var two = new LibraryEntry(new LibraryMod("two", "Dog", ModKind.AddIn, [new LibraryModFile("a.gda", 1, "2")], "dog"), "s", "sha", _day);

        var copies = ModCopies.Find([one, two]);

        Assert.All(copies.Values, c => Assert.False(c.HoldsAll));
        Assert.All(copies.Values, c => Assert.False(c.IsPart));
    }

    private static LibraryEntry Entry(string id, string name, int day, params (string Path, string Sha)[] files) =>
        new(new LibraryMod(id, name, ModKind.Override, [.. files.Select(f => new LibraryModFile(f.Path, 1, f.Sha))]), "source", "sha", _day.AddDays(day));
}
