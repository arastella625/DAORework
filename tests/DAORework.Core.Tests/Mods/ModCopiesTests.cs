using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ModCopiesTests
{
    private static readonly DateTimeOffset _day = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

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
    public void Find_ModsWithNothingForTheGame_AreNotCopies()
    {
        var copies = ModCopies.Find([Entry("a", 0, null, ("readme.txt", "a")), Entry("b", 1, null, ("readme.txt", "a"))]);

        Assert.Empty(copies);
    }

    private static LibraryEntry Entry(string id, int days, string? uid, params (string Path, string Sha)[] files)
    {
        var kind = uid is null ? ModKind.Override : ModKind.AddIn;
        var mod = new LibraryMod(id, id, kind, [.. files.Select(f => new LibraryModFile(f.Path, 1, f.Sha))], uid);
        return new LibraryEntry(mod, id, id, _day.AddDays(days));
    }
}
