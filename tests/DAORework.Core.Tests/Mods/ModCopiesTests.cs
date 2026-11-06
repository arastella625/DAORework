using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ModCopiesTests
{
    private static readonly DateTimeOffset _day = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Find_AnEntryWithOnlyAReadme_IsPartOfTheModOfItsName()
    {
        var whole = Named("whole", "Fantasy", ("hair.dds", "aa"), ("readme.txt", "bb"));
        var readme = Named("readme", "Fantasy", ("for chargenmorphcfg.txt", "cc"));

        var copies = ModCopies.Find([whole, readme]);

        Assert.True(copies["whole"].HoldsAll);
        Assert.True(copies["readme"].IsPart);
        Assert.Equal(copies["whole"].Group, copies["readme"].Group);
    }

    [Fact]
    public void Find_AMacLeftoverUnderAnotherSpelling_IsPartToo()
    {
        var copies = ModCopies.Find([Named("whole", "Additional Hairs for DA (Part 1)", ("hair.dds", "aa")), Named("left", "Additional Hairs for DA Part 1", (".DS_Store", "cc"))]);

        Assert.True(copies["left"].IsPart);
    }

    [Fact]
    public void Find_TwoDifferentModsOfOneName_ShareALeftoverOnlyOnce()
    {
        var copies = ModCopies.Find([Named("v1", "Fantasy", ("a.dds", "aa")), Named("v2", "Fantasy", ("b.dds", "bb")), Named("readme", "Fantasy", ("readme.txt", "cc"))]);

        Assert.Equal(2, copies.Count);
        Assert.Equal(copies["readme"].Group, copies.Single(c => c.Key != "readme").Value.Group);
    }

    [Fact]
    public void Find_NamesWithNoLettersInThem_DoNotMatch()
    {
        Assert.Empty(ModCopies.Find([Named("whole", "!!!", ("a.dds", "aa")), Named("left", "---", ("readme.txt", "cc"))]));
    }

    [Fact]
    public void Find_AReadmeWithNoModOfItsName_IsLeftAlone()
    {
        Assert.Empty(ModCopies.Find([Named("whole", "Fantasy", ("hair.dds", "aa")), Named("readme", "Something Else", ("readme.txt", "cc"))]));
    }

    [Fact]
    public void Find_AddInsWithOneUid_AreCopiesEvenWithDifferentFiles_NewestFirst()
    {
        var older = Entry("older", 0, "Dog_UID", ("Contents/a.gda", "1"));
        var newer = Entry("newer", 1, "dog_uid", ("Contents/a.gda", "2"), ("Contents/b.gda", "3"));

        var copies = ModCopies.Find([older, newer]);

        Assert.Equal(0, copies["newer"].Rank);
        Assert.Equal(1, copies["older"].Rank);
        Assert.Equal(copies["newer"].Group, copies["older"].Group);
        Assert.Equal(2, copies["older"].Count);
    }

    [Fact]
    public void Find_OverrideMods_AreCopiesOnlyWithTheSameModFiles()
    {
        var first = Entry("first", 0, null, ("hair.dds", "1"), ("readme.txt", "a"));
        var same = Entry("same", 1, null, ("hair.dds", "1"));
        var newVersion = Entry("new-version", 2, null, ("hair.dds", "2"));

        var copies = ModCopies.Find([first, same, newVersion]);

        Assert.Equal(["first", "same"], copies.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Find_TheSameModManagedFromInsideItsFolder_IsACopy_UnderAnyName()
    {
        var fromTheFolder = Entry("Anto Hairstyles", 0, null, ("Anto Hairstyles/Coolsims/df/hair.mmh", "1"), ("Anto Hairstyles/Coolsims/df/hair.dds", "2"));
        var fromInsideIt = Entry("Anto Hairstyles 2", 1, null, ("Coolsims/df/hair.dds", "2"), ("Coolsims/df/hair.mmh", "1"));

        var copies = ModCopies.Find([fromTheFolder, fromInsideIt]);

        Assert.Equal(2, copies.Count);
        Assert.Equal("Anto Hairstyles 2", copies["Anto Hairstyles"].AlsoNamed);
        Assert.Equal("Anto Hairstyles", copies["Anto Hairstyles 2"].AlsoNamed);
    }

    [Fact]
    public void Find_TheSameContentUnderDifferentFileNames_IsNotACopy()
    {
        var copies = ModCopies.Find([Entry("red", 0, null, ("red_hair.dds", "1")), Entry("blue", 1, null, ("blue_hair.dds", "1"))]);

        Assert.Empty(copies);
    }

    [Fact]
    public void Find_ModsWithNothingForTheGame_AreNotCopies()
    {
        var copies = ModCopies.Find([Entry("a", 0, null, ("readme.txt", "a")), Entry("b", 1, null, ("readme.txt", "a"))]);

        Assert.Empty(copies);
    }

    private static LibraryEntry Named(string id, string name, params (string Path, string Sha)[] files) =>
        new(new LibraryMod(id, name, ModKind.Override, [.. files.Select(f => new LibraryModFile(f.Path, 1, f.Sha))]), "source", "sha", DateTimeOffset.UnixEpoch);

    private static LibraryEntry Entry(string id, int days, string? uid, params (string Path, string Sha)[] files)
    {
        var kind = uid is null ? ModKind.Override : ModKind.AddIn;
        var mod = new LibraryMod(id, id, kind, [.. files.Select(f => new LibraryModFile(f.Path, 1, f.Sha))], uid);
        return new LibraryEntry(mod, id, id, _day.AddDays(days));
    }
}
