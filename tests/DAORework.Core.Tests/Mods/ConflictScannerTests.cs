using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Core.Tests.Mods;

public sealed class ConflictScannerTests
{
    [Fact]
    public void Scan_NoOverlaps_DeploysEverythingAndReportsNothing()
    {
        var report = Scan([Mods.Override("a", "one.dds"), Mods.Override("b", "two.dds")]);

        Assert.Equal(2, report.Deploy.Count);
        Assert.Empty(report.OverrideConflicts);
        Assert.Empty(report.PathCollisions);
        Assert.False(report.IsBlocked);
    }

    [Fact]
    public void Scan_SameOverrideFileName_LaterModWinsAndOnlyItsCopyIsDeployed()
    {
        var report = Scan([Mods.Override("low", "textures/HAIR.dds#1"), Mods.Override("mid", "hair.dds#2"), Mods.Override("high", "hair/hair.dds#3")]);

        var conflict = Assert.Single(report.OverrideConflicts);
        Assert.Equal("high", conflict.Winner.ModId);
        Assert.Equal(["low", "mid"], conflict.Overridden.Select(o => o.ModId));
        Assert.False(conflict.SameContent);
        Assert.Equal(["high"], report.Deploy.Select(d => d.ModId));
        Assert.False(report.IsBlocked);
    }

    [Fact]
    public void Scan_SameNameSameContent_IsMarkedHarmless()
    {
        var report = Scan([Mods.Override("a", "shared.gda#abc"), Mods.Override("b", "shared.gda#ABC")]);

        Assert.True(Assert.Single(report.OverrideConflicts).SameContent);
    }

    [Fact]
    public void Scan_TwoModsWritingTheSameAddInFolder_IsABlockingCollision()
    {
        var report = Scan([Mods.AddIn("fixpack-35", "qwinn_fixpack_3", "Contents/addins/qwinn_fixpack_3/core/x.ncs"), Mods.AddIn("fixpack-352", "qwinn_fixpack_3", "Contents/addins/qwinn_fixpack_3/core/x.ncs")]);

        var collision = Assert.Single(report.PathCollisions);
        Assert.Equal("AddIns/qwinn_fixpack_3/core/x.ncs", collision.TargetPath);
        Assert.Equal(["fixpack-35", "fixpack-352"], collision.Files.Select(f => f.ModId));
        Assert.True(report.IsBlocked);
    }

    [Fact]
    public void Scan_ManagedFileNamedLikeAHandPlacedOne_IsReportedAsShadowed()
    {
        var report = ConflictScanner.Scan([Mods.Override("a", "textures/hair.dds", "eyes.dds")], [@"My Mods\HAIR.dds", "other/unrelated.dds"]).Value;

        var shadow = Assert.Single(report.UnmanagedShadows);
        Assert.Equal("hair.dds", shadow.Managed.FileName);
        Assert.Equal([@"My Mods\HAIR.dds"], shadow.UnmanagedPaths);
    }

    [Fact]
    public void Scan_OverrideFileNamedLikeAnAddInFile_IsReportedAsOverridingIt()
    {
        var report = Scan([Mods.AddIn("dog", "Extra_Dog_Slot", "Contents/addins/Extra_Dog_Slot/module/data/dog.gda"), Mods.Override("tweak", "dog.gda")]);

        var overridden = Assert.Single(report.AddInFilesOverridden);
        Assert.Equal("tweak", overridden.Override.ModId);
        Assert.Equal("AddIns/Extra_Dog_Slot/module/data/dog.gda", overridden.AddInFile.TargetPath);
        Assert.Empty(report.OverrideConflicts);
    }

    [Fact]
    public void Scan_AddInManifestIsNotDeployed()
    {
        var report = Scan([Mods.AddIn("dog", "Extra_Dog_Slot", "Manifest.xml", "Contents/addins/Extra_Dog_Slot/a.gda")]);

        Assert.Equal(["AddIns/Extra_Dog_Slot/a.gda"], report.Deploy.Select(d => d.TargetPath));
    }

    [Fact]
    public void Scan_TheSameModTwice_Fails()
    {
        var result = ConflictScanner.Scan([Mods.Override("a", "x.dds"), Mods.Override("A", "y.dds")], []);

        Assert.Equal(DiagnosticCodes.ModListDuplicate, result.Error!.Code);
    }

    [Fact]
    public void Scan_AModWithAnUnsafePath_FailsWithThatError()
    {
        var result = ConflictScanner.Scan([Mods.Override("a", "../x.dds")], []);

        Assert.Equal(DiagnosticCodes.ModPathInvalid, result.Error!.Code);
    }

    [Fact]
    public void Scan_NoMods_DeploysNothing()
    {
        Assert.Empty(Scan([]).Deploy);
    }

    private static ConflictReport Scan(IReadOnlyList<LibraryMod> mods) => ConflictScanner.Scan(mods, []).Value;
}
