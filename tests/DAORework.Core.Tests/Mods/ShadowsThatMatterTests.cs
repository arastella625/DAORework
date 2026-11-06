using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>Which files DAORework doesn't manage are worth a warning (P2.6-20).</summary>
public sealed class ShadowsThatMatterTests
{
    private const string Prefix = "packages/core/override/";

    [Fact]
    public void Keep_AFileWithTheModsContent_IsDropped_WhateverTheHashesCase()
    {
        var kept = ShadowsThatMatter.Keep([Shadow("new", "Old/hair.dds")], [], new Dictionary<string, string> { [Prefix + "Old/hair.dds"] = "ABC" });

        Assert.Empty(kept);
    }

    [Fact]
    public void Keep_AFileThatDiffers_OrOneOfTwoThatDiffers_Stays()
    {
        var hashes = new Dictionary<string, string> { [Prefix + "Old/hair.dds"] = "abc", [Prefix + "Other/hair.dds"] = "def" };

        Assert.Single(ShadowsThatMatter.Keep([Shadow("new", "Old/hair.dds", "Other/hair.dds")], [], hashes));
    }

    [Fact]
    public void Keep_AFileThatCouldNotBeCompared_Stays()
    {
        Assert.Single(ShadowsThatMatter.Keep([Shadow("new", "Old/hair.dds")], [], new Dictionary<string, string>()));
    }

    [Fact]
    public void Keep_AModAlreadyOn_IsNotMentionedAgain()
    {
        Assert.Empty(ShadowsThatMatter.Keep([Shadow("on", "Old/hair.dds")], ["ON"], new Dictionary<string, string>()));
    }

    [Fact]
    public void PathsToCompare_NamesOnlyWhatTheChangeAdds_FromTheDocumentsFolder_OnceEach()
    {
        var paths = ShadowsThatMatter.PathsToCompare([Shadow("on", "Kept/eyes.dds"), Shadow("new", @"Old\hair.dds"), Shadow("newer", "Old/hair.dds")], ["on"]);

        Assert.Equal(Prefix + "Old/hair.dds", Assert.Single(paths));
    }

    private static UnmanagedShadow Shadow(string modId, params string[] paths) =>
        new(new DeployedFile(modId, new LibraryModFile("hair.dds", 1, "abc"), Prefix + "DAORework/" + modId + "/hair.dds", IsOverride: true), paths);
}
