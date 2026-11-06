using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Library;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Application.Tests.Mods.Library;

public sealed class FindModsInsideTests
{
    private static readonly IReadOnlyList<LibraryEntry> _entries =
    [
        Entry("zdf", "small.erf", "s"),
        Entry("whole", "big.erf", "b"),
    ];

    [Fact]
    public void Execute_ReadsEachArchiveOnce_HoweverOftenTheListIsRead()
    {
        var archives = new FakeArchives();
        var find = new FindModsInside(archives);

        find.Execute(_entries);
        var found = find.Execute(_entries);

        Assert.Equal(2, archives.Reads);
        Assert.Equal("whole", found["zdf"].WholeId);
    }

    [Fact]
    public void Execute_AnArchiveInUse_IsReadAgainNextTime()
    {
        var archives = new FakeArchives { Unreadable = true };
        var find = new FindModsInside(archives);
        Assert.Empty(find.Execute(_entries));

        archives.Unreadable = false;

        Assert.Equal("whole", find.Execute(_entries)["zdf"].WholeId);
    }

    private static LibraryEntry Entry(string id, string file, string hash) =>
        new(new LibraryMod(id, id, ModKind.Override, [new LibraryModFile(file, 4, hash)]), id + ".zip", "sha", DateTimeOffset.UnixEpoch);

    private sealed class FakeArchives : IErfContentsReader
    {
        public int Reads { get; private set; }

        public bool Unreadable { get; set; }

        public Result<IReadOnlyList<string>> ReadNames(string modId, string relativePath)
        {
            Reads++;
            return Unreadable
                ? Result.Failure<IReadOnlyList<string>>(new Error(DiagnosticCodes.ModErfUnreadable, "In use."))
                : Result.Success<IReadOnlyList<string>>(relativePath == "small.erf" ? ["a.dds"] : ["a.dds", "b.dds"]);
        }
    }
}
