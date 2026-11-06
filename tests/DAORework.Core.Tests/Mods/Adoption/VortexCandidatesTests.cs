using DAORework.Core.Catalog;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods.Adoption;

/// <summary>The mods a Vortex record names, as the adoption scan lists them (ADR-0028 §2).</summary>
public sealed class VortexCandidatesTests
{
    private const string Override = "packages/core/override/";
    private const string Hairs = "Anto Hairstyles 2-3480-1-2-3";
    private const string Eyes = "EyesssReplaceDAO by ViLiSSa-3537-1-0";

    [Theory]
    [InlineData(Hairs, "Anto Hairstyles 2")]
    [InlineData("14 New Face Tattoos-3500-", "14 New Face Tattoos")]
    [InlineData("Lady of the forest HD -4227-1-0", "Lady of the forest HD")]
    [InlineData("DA2 Style 2-Handed Weapons v1_0c-2455-1-0c", "DA2 Style 2-Handed Weapons v1_0c")]
    [InlineData("Better Asala - Silverite-5053-1-0-1556939056", "Better Asala - Silverite")]
    [InlineData("Eyeshadow-4868--10", "Eyeshadow")]
    [InlineData("Fantasy-3500-1a", "Fantasy")]
    [InlineData("FrostyModManager", "FrostyModManager")]
    public void Display_LeavesOffTheNexusIdAndVersion(string folder, string name) =>
        Assert.Equal(name, VortexModNames.Display(folder));

    [Fact]
    public void Scan_AVortexModWithOnlyAReadmeLeft_IsNoMod_AndIsNotOfferedAsLooseFiles()
    {
        var snapshot = Snapshot(folders: [], loose: ["readme.txt"], new VortexRecord([Placed("readme.txt", Hairs)]));

        Assert.Empty(Scan(snapshot).Candidates);
    }

    [Fact]
    public void Scan_EachVortexMod_IsACandidateOfItsFilesWhereverTheySit_AndTheyLeaveTheFolderAndLooseFiles()
    {
        var snapshot = Snapshot(
            folders: [("next part", ["next part/hair1.dds", "next part/eyes.dds", "next part/mine.dds"])],
            loose: ["hair2.dds"],
            new VortexRecord([Placed("next part/hair1.dds", Hairs), Placed("hair2.dds", Hairs), Placed("next part/eyes.dds", Eyes), Placed("gone.dds", Eyes)]));

        var scan = Scan(snapshot);

        Assert.Equal(["override:next part", $"vortex:{Hairs}", $"vortex:{Eyes}"], scan.Candidates.Select(c => c.Id).OrderBy(id => id[0]).ThenBy(id => id.Length));
        var hairs = scan.Candidates.Single(c => c.Id == $"vortex:{Hairs}");
        Assert.Equal((AdoptionCandidateKind.VortexMod, "Anto Hairstyles 2", "packages/core/override"), (hairs.Kind, hairs.Name, hairs.Path));
        Assert.Equal([Override + "hair2.dds", Override + "next part/hair1.dds"], hairs.Files.Select(f => f.Path));
        Assert.Equal([Override + "next part/eyes.dds"], scan.Candidates.Single(c => c.Id == $"vortex:{Eyes}").Files.Select(f => f.Path));
        Assert.Equal([Override + "next part/mine.dds"], scan.Candidates.Single(c => c.Id == "override:next part").Files.Select(f => f.Path));
        Assert.DoesNotContain(scan.Candidates, c => c.Kind == AdoptionCandidateKind.LooseOverrideFiles);
    }

    [Fact]
    public void Scan_AModTheStagingFolderStillHolds_AtItsTarget_IsLive_AndTheOthersAreLeftBehind()
    {
        var record = new VortexRecord([Placed("hair.dds", Hairs), Placed("eyes.dds", Eyes)])
        {
            AtItsTarget = true,
            StagedMods = new HashSet<string>([Hairs], StringComparer.OrdinalIgnoreCase),
        };

        var scan = Scan(Snapshot(folders: [], loose: ["hair.dds", "eyes.dds"], record));

        Assert.True(scan.Candidates.Single(c => c.Id == $"vortex:{Hairs}").DeployedByVortex);
        Assert.False(scan.Candidates.Single(c => c.Id == $"vortex:{Eyes}").DeployedByVortex);
    }

    [Fact]
    public void Scan_ARecordThatHasMoved_LeavesEveryModBehind()
    {
        var record = new VortexRecord([Placed("hair.dds", Hairs)]) { StagedMods = new HashSet<string>([Hairs], StringComparer.OrdinalIgnoreCase) };

        var candidate = Assert.Single(Scan(Snapshot(folders: [], loose: ["hair.dds"], record)).Candidates);

        Assert.False(candidate.DeployedByVortex);
    }

    [Fact]
    public void Scan_DaoreworksOwnFolder_IsNeverVortexs_AndAFileTwoRecordsList_IsTheFirstsOnly()
    {
        var snapshot = Snapshot(
            folders: [("DAORework", ["DAORework/m/hair.dds"])],
            loose: ["eyes.dds"],
            new VortexRecord([Placed("DAORework/m/hair.dds", Hairs), Placed("eyes.dds", Eyes)]),
            new VortexRecord([Placed("eyes.dds", Hairs)]));

        var candidate = Assert.Single(Scan(snapshot).Candidates);

        Assert.Equal($"vortex:{Eyes}", candidate.Id);
    }

    private static VortexPlacedFile Placed(string path, string mod) => new(Override + path, mod);

    private static AdoptionScan Scan(HandInstalledSnapshot snapshot) => AdoptionScanner.Scan(snapshot, DeploymentRecord.Empty, new KnownItemsCatalog(1, [], []));

    private static HandInstalledSnapshot Snapshot((string Name, string[] Files)[] folders, string[] loose, params VortexRecord[] records) =>
        new(
            [],
            new Dictionary<string, IReadOnlyList<HandInstalledFile>>(StringComparer.OrdinalIgnoreCase),
            folders.ToDictionary(f => f.Name, f => (IReadOnlyList<HandInstalledFile>)f.Files.Select(p => new HandInstalledFile(Override + p, 10)).ToList(), StringComparer.OrdinalIgnoreCase),
            loose.Select(p => new HandInstalledFile(Override + p, 10)).ToList())
        {
            VortexRecords = records,
        };
}
