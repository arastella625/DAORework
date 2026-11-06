using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>What a mod's row says about the files it shares (P2.6-33).</summary>
public sealed class ConflictMarksTests
{
    [Fact]
    public void For_AWinnerALoserAndBoth()
    {
        var marks = ConflictMarks.For([Conflict("a.dds", "b", "1", ("a", "2")), Conflict("c.dds", "c", "1", ("b", "2"))]);

        Assert.Equal("loses", marks["a"]);
        Assert.Equal("wins and loses", marks["b"]);
        Assert.Equal("wins", marks["c"]);
    }

    [Fact]
    public void For_IdenticalCopies_AreNotMarked()
    {
        Assert.Empty(ConflictMarks.For([Conflict("a.dds", "b", "1", ("a", "1"))]));
    }

    private static OverrideConflict Conflict(string name, string winner, string sha, (string Mod, string Sha) other) =>
        new(name, File(name, winner, sha), [File(name, other.Mod, other.Sha)]);

    private static DeployedFile File(string name, string mod, string sha) =>
        new(mod, new LibraryModFile(name, 1, sha), $"packages/core/override/DAORework/{mod}/{name}", IsOverride: true);
}
