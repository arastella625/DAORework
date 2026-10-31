using DAORework.Core.Catalog;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods.Adoption;

public sealed class AdoptionScannerTests
{
    private static readonly KnownItem _witchHunt = new(
        "dlc-witch-hunt",
        "Witch Hunt",
        KnownItemKind.Official,
        "https://example.org/dao",
        "Proprietary",
        [new KnownItemFingerprint(FingerprintKind.AddInUid, "DAO_PRC_STR"), new KnownItemFingerprint(FingerprintKind.AddInUid, "DAO_PRC_STR_DAO")]);

    private static readonly KnownItem _dogSlot = Item("extra-dog-slot", FingerprintKind.AddInUid, "Extra_Dog_Slot");

    private static readonly KnownItem _hair = Item("better-hair", FingerprintKind.OverrideFile, "hair.dds");

    private static readonly KnownItemsCatalog _catalog = new(1, [_witchHunt, _dogSlot, _hair], []);

    [Fact]
    public void Scan_RegisteredAddInWithItsFolder_IsACandidateWithTitleAndCatalogEntry()
    {
        var snapshot = Snapshot(addIns: [new RegisteredAddIn("Extra_Dog_Slot", "Extra Dog Slot v1.41")], addInFolders: Folders(("Extra_Dog_Slot", ["AddIns/Extra_Dog_Slot/core/dog.gda"])));

        var candidate = Assert.Single(Scan(snapshot).Candidates);

        Assert.Equal("addin:Extra_Dog_Slot", candidate.Id);
        Assert.Equal(AdoptionCandidateKind.AddIn, candidate.Kind);
        Assert.Equal("Extra Dog Slot v1.41", candidate.Name);
        Assert.Equal("AddIns/Extra_Dog_Slot", candidate.Path);
        Assert.Equal(10, candidate.SizeBytes);
        Assert.Same(_dogSlot, candidate.CatalogItem);
    }

    [Fact]
    public void Scan_OfficialDlc_IsNeverACandidateEvenWithAFolderInDocuments()
    {
        var snapshot = Snapshot(
            addIns: [new RegisteredAddIn("DAO_PRC_STR", "Witch Hunt"), new RegisteredAddIn("dao_prc_str_dao", "Witchcraft")],
            addInFolders: Folders(("DAO_PRC_STR", ["AddIns/DAO_PRC_STR/module.erf"])));

        var scan = Scan(snapshot);

        Assert.Empty(scan.Candidates);
        Assert.All(scan.Exclusions, e => Assert.Equal(AdoptionExclusionReason.Official, e.Reason));
        Assert.Equal(2, scan.Exclusions.Count);
    }

    [Fact]
    public void Scan_AddInWithoutAFolderAndAFolderWithoutAnEntry_AreExcludedWithTheirReasons()
    {
        var snapshot = Snapshot(addIns: [new RegisteredAddIn("Elsewhere", "Elsewhere")], addInFolders: Folders(("Orphan", ["AddIns/Orphan/x.erf"])));

        var scan = Scan(snapshot);

        Assert.Empty(scan.Candidates);
        Assert.Equal(
            [("addin:Elsewhere", AdoptionExclusionReason.NoFolderInDocuments), ("addin:Orphan", AdoptionExclusionReason.NotRegistered)],
            scan.Exclusions.Select(e => (e.Id, e.Reason)));
    }

    [Fact]
    public void Scan_AddInsDaoreworkInstalled_AreLeftOut()
    {
        var snapshot = Snapshot(
            addIns: [new RegisteredAddIn("Managed", "Managed")],
            addInFolders: Folders(("Managed", ["AddIns/Managed/a.erf"]), ("Placed", ["AddIns/Placed/b.erf"])));
        var record = new DeploymentRecord(["m"], [new RecordedFile("AddIns/Placed/b.erf", "hash", "m")], [new RecordedAddIn("Managed", "m")]);

        var scan = AdoptionScanner.Scan(snapshot, record, _catalog);

        Assert.Empty(scan.Candidates);
        Assert.Empty(scan.Exclusions);
    }

    [Fact]
    public void Scan_OverrideFoldersAndLooseFiles_AreCandidatesButNotDaoreworksOwnFolder()
    {
        var snapshot = Snapshot(
            overrideFolders: Folders(("Better Hair", ["packages/core/override/Better Hair/hair.dds"]), ("DAORework", ["packages/core/override/DAORework/m/a.dds"]), ("Empty", [])),
            loose: [new HandInstalledFile("packages/core/override/loose.uti", 4)]);

        var scan = Scan(snapshot with { VortexDeployedOverride = true });

        Assert.Equal(["override:(loose)", "override:Better Hair"], scan.Candidates.Select(c => c.Id));
        var hair = scan.Candidates[1];
        Assert.Equal("packages/core/override/Better Hair", hair.Path);
        Assert.Same(_hair, hair.CatalogItem);
        Assert.Equal("packages/core/override", scan.Candidates[0].Path);
        Assert.All(scan.Candidates, c => Assert.True(c.DeployedByVortex));
    }

    [Fact]
    public void Scan_VortexInAddInsOnly_FlagsOnlyAddIns()
    {
        var snapshot = Snapshot(
            addIns: [new RegisteredAddIn("Mod", "Mod")],
            addInFolders: Folders(("Mod", ["AddIns/Mod/a.erf"])),
            loose: [new HandInstalledFile("packages/core/override/loose.uti", 4)]);

        var scan = Scan(snapshot with { VortexDeployedAddIns = true });

        Assert.True(scan.Candidates.Single(c => c.Kind == AdoptionCandidateKind.AddIn).DeployedByVortex);
        Assert.False(scan.Candidates.Single(c => c.Kind == AdoptionCandidateKind.LooseOverrideFiles).DeployedByVortex);
    }

    private static KnownItem Item(string id, FingerprintKind kind, string value) =>
        new(id, id, KnownItemKind.Mod, $"https://example.org/{id}", "Author's permission", [new KnownItemFingerprint(kind, value)]);

    private static AdoptionScan Scan(HandInstalledSnapshot snapshot) => AdoptionScanner.Scan(snapshot, DeploymentRecord.Empty, _catalog);

    private static Dictionary<string, IReadOnlyList<HandInstalledFile>> Folders(params (string Name, string[] Files)[] folders) =>
        folders.ToDictionary(f => f.Name, f => (IReadOnlyList<HandInstalledFile>)f.Files.Select(p => new HandInstalledFile(p, 10)).ToList(), StringComparer.OrdinalIgnoreCase);

    private static HandInstalledSnapshot Snapshot(
        IReadOnlyList<RegisteredAddIn>? addIns = null,
        Dictionary<string, IReadOnlyList<HandInstalledFile>>? addInFolders = null,
        Dictionary<string, IReadOnlyList<HandInstalledFile>>? overrideFolders = null,
        IReadOnlyList<HandInstalledFile>? loose = null) =>
        new(addIns ?? [], addInFolders ?? Folders(), overrideFolders ?? Folders(), loose ?? []);
}
