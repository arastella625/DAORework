using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ModsInsideTests
{
    private static readonly Dictionary<string, IReadOnlyList<string>> _archives = new()
    {
        ["small.erf"] = ["a.dds", "b.dds"],
        ["big.erf"] = ["a.dds", "b.dds", "c.dds"],
        ["other.erf"] = ["a.dds", "z.dds"],
    };

    [Fact]
    public void Find_EveryFileInABiggerModOfAnotherName_NamesIt()
    {
        var found = Find(Mod("zdf", "ZDF", "a.dds#1", "b.dds#2"), Mod("whole", "ZDF Complete", "a.dds#1", "b.dds#2", "c.dds#3"));

        Assert.Equal(new ModInside("whole", "ZDF Complete", ByName: false), Assert.Single(found).Value);
        Assert.True(found.ContainsKey("zdf"));
    }

    [Fact]
    public void Find_OneFileDiffers_IsNotInside() =>
        Assert.Empty(Find(Mod("zdf", "ZDF", "a.dds#1", "b.dds#9"), Mod("whole", "ZDF Complete", "a.dds#1", "b.dds#2", "c.dds#3")));

    [Fact]
    public void Find_TheSameFiles_IsACopy_NotInside() =>
        Assert.Empty(Find(Mod("one", "One", "a.dds#1"), Mod("two", "Two", "a.dds#1")));

    [Fact]
    public void Find_ThreeMods_NamesTheBiggest()
    {
        var found = Find(Mod("s", "Small", "a.dds#1"), Mod("m", "Middle", "a.dds#1", "b.dds#2"), Mod("l", "Large", "a.dds#1", "b.dds#2", "c.dds#3"));

        Assert.Equal("l", found["s"].WholeId);
        Assert.Equal("l", found["m"].WholeId);
        Assert.False(found.ContainsKey("l"));
    }

    [Fact]
    public void Find_AnArchiveWhoseResourcesAreInABiggerArchive_IsInside_ByName()
    {
        var found = Find(Mod("zdf", "ZDF", "small.erf#1"), Mod("whole", "ZDF Complete", "big.erf#2"));

        Assert.True(found["zdf"].ByName);
    }

    [Fact]
    public void Find_AnArchiveWhoseResourcesAreLooseInABiggerMod_IsInside_ByName() =>
        Assert.True(Find(Mod("zdf", "ZDF", "small.erf#1"), Mod("whole", "ZDF Complete", "a.dds#1", "b.dds#2", "c.dds#3"))["zdf"].ByName);

    [Fact]
    public void Find_AnArchiveWithAResourceTheOtherLacks_IsNotInside() =>
        Assert.Empty(Find(Mod("zdf", "ZDF", "other.erf#1"), Mod("whole", "ZDF Complete", "big.erf#2")));

    [Fact]
    public void Find_AnArchiveThatCantBeRead_ComparesByItsHashAlone() =>
        Assert.Empty(Find(Mod("zdf", "ZDF", "broken.erf#1"), Mod("whole", "ZDF Complete", "big.erf#2")));

    [Fact]
    public void Find_AddIns_AreLeftToTheirUid()
    {
        var addIn = Mod("addin", "Add-in", "a.dds#1") with { Kind = ModKind.AddIn, AddInUid = "uid" };

        Assert.Empty(Find(addIn, Mod("whole", "Whole", "a.dds#1", "b.dds#2")));
    }

    private static IReadOnlyDictionary<string, ModInside> Find(params LibraryMod[] mods) =>
        ModsInside.Find(
            [.. mods.Select(m => new LibraryEntry(m, m.Name + ".zip", "sha", DateTimeOffset.UnixEpoch))],
            (_, file) => _archives.GetValueOrDefault(file.RelativePath));

    private static LibraryMod Mod(string id, string name, params string[] files) =>
        new(id, name, ModKind.Override, [.. files.Select(f => new LibraryModFile(f.Split('#')[0], 4, f.Split('#')[1]))]);
}
