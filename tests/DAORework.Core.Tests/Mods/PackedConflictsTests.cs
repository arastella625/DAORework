using DAORework.Core.Mods.Conflicts;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>A shared resource is found however it is packed (P3-07).</summary>
public sealed class PackedConflictsTests
{
    [Fact]
    public void Find_TwoArchivesHoldingOneResource_IsAConflict_WhateverItsCase()
    {
        var conflict = Assert.Single(PackedConflicts.Find([new("a", ["Hair.dds", "only_a.gda"]), new("b", ["hair.DDS"])], []));

        Assert.Equal(["a", "b"], conflict.ModIds);
    }

    [Fact]
    public void Find_AnArchiveAndALooseFile_IsAConflict()
    {
        var conflict = Assert.Single(PackedConflicts.Find([new("a", ["hair.dds"])], [("b", "hair.dds")]));

        Assert.Equal(["a", "b"], conflict.ModIds);
    }

    [Fact]
    public void Find_ModsThatShareNothing_HaveNoConflict()
    {
        Assert.Empty(PackedConflicts.Find([new("a", ["x.dds"]), new("b", ["y.dds"])], [("c", "z.dds")]));
    }

    [Fact]
    public void Find_OneModHoldingAFileTwice_IsNoConflict()
    {
        Assert.Empty(PackedConflicts.Find([new("a", ["x.dds"]), new("a", ["x.dds"])], [("a", "x.dds")]));
    }

    [Fact]
    public void Find_TwoLooseFiles_AreTheScannersJob()
    {
        Assert.Empty(PackedConflicts.Find([], [("a", "x.dds"), ("b", "x.dds")]));
    }
}
