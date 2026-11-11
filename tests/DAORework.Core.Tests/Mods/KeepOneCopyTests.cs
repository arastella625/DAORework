using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class KeepOneCopyTests
{
    private const string Here = "here";
    private const string There = "there";

    private static readonly LibraryEntry _kept = Entry("kept", ("hair.dds", "aa", 10));
    private static readonly LibraryEntry _copy = Entry("copy", ("hair.dds", "aa", 10));

    [Fact]
    public void Plan_NothingOn_ChangesNothingInTheGame_AndCountsTheSpace()
    {
        var plan = KeepOneCopy.Plan(_kept, [_copy], Here, [new FolderMods(Here, [], false)]);

        Assert.Equal(10, plan.BytesFreed);
        Assert.Null(plan.SwappedOrder);
        Assert.Empty(plan.Problems);
        Assert.Empty(plan.OnlyInRemoved);
    }

    [Fact]
    public void Plan_ARemovedCopyOn_GivesItsPlaceToTheKeptOne()
    {
        var plan = KeepOneCopy.Plan(_kept, [_copy], Here, [new FolderMods(Here, ["a", "copy", "b"], false)]);

        Assert.Equal(["a", "kept", "b"], plan.SwappedOrder);
        Assert.Equal("copy", plan.SwappedOut);
    }

    [Fact]
    public void Plan_BothOn_TheKeptOneStaysWhereItIs()
    {
        var plan = KeepOneCopy.Plan(_kept, [_copy], Here, [new FolderMods(Here, ["copy", "a", "kept"], false)]);

        Assert.Equal(["a", "kept"], plan.SwappedOrder);
    }

    [Fact]
    public void Plan_TwoRemovedCopiesOn_TheKeptOneTakesTheFirstPlace()
    {
        var plan = KeepOneCopy.Plan(_kept, [_copy, Entry("third")], Here, [new FolderMods(Here, ["copy", "a", "third"], false)]);

        Assert.Equal(["kept", "a"], plan.SwappedOrder);
    }

    [Fact]
    public void Plan_FilesOnlyInARemovedCopy_AreNamed_ButNotReadmes()
    {
        var other = Entry("copy", ("Hair/hair.dds", "aa", 10), ("Hair/new.dds", "bb", 5), ("readme.txt", "cc", 1));

        Assert.Equal(["new.dds"], KeepOneCopy.Plan(_kept, [other], Here, []).OnlyInRemoved);
    }

    [Fact]
    public void Plan_ACopyOnInAnotherFolder_StopsIt()
    {
        var plan = KeepOneCopy.Plan(_kept, [_copy], Here, [new FolderMods(Here, [], false), new FolderMods(There, ["copy"], false)]);

        Assert.Contains(There, Assert.Single(plan.Problems).Title, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_NoFolderSelected_ACopyOnAnywhereStopsIt()
    {
        var plan = KeepOneCopy.Plan(_kept, [_copy], null, [new FolderMods(There, ["copy"], false)]);

        Assert.Single(plan.Problems);
        Assert.Null(plan.SwappedOrder);
    }

    [Fact]
    public void Plan_AChangeThatDidNotFinish_StopsIt()
    {
        var plan = KeepOneCopy.Plan(_kept, [_copy], Here, [new FolderMods(There, [], true)]);

        Assert.Contains("didn't finish", Assert.Single(plan.Problems).Title, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovedMods_NamesWhatLeft_ButNeverAFix()
    {
        var removed = RemovedMods.Ids(["hair-pack-1a2b3c4d", "kept", "fixes/dxvk-2-3-aaaaaaaa"], ["kept"]);

        Assert.Equal(["hair-pack-1a2b3c4d"], removed);
        Assert.Equal("Hair pack", ModIds.ReadableName(removed[0]));
    }

    private static LibraryEntry Entry(string id, params (string Path, string Sha, long Size)[] files) =>
        new(new LibraryMod(id, id, ModKind.Override, [.. files.Select(f => new LibraryModFile(f.Path, f.Size, f.Sha))]), "source", "sha", DateTimeOffset.UnixEpoch);
}
