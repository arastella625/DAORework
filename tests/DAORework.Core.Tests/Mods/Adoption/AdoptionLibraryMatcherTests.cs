using DAORework.Core.Catalog;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Core.Tests.Mods.Adoption;

public sealed class AdoptionLibraryMatcherTests
{
    private static readonly LibraryMod _dogSlot = new(
        "extra-dog-slot-1",
        "Extra Dog Slot",
        ModKind.AddIn,
        [File("Manifest.xml", "m"), File("Contents/addins/Extra_Dog_Slot/core/dog.gda", "dog"), File("Contents/packages/core/override/whistle.uti", "whistle")],
        "Extra_Dog_Slot");

    private static readonly LibraryMod _hair = new("better-hair-1", "Better Hair", ModKind.Override, [File("Better Hair/hair.dds", "hair"), File("Better Hair/brow.dds", "brow")]);

    [Fact]
    public void Match_AddInWhereDaUpdaterPutsItsFiles_IsExact()
    {
        var snapshot = Snapshot(addIns: [("Extra_Dog_Slot", ["AddIns/Extra_Dog_Slot/core/dog.gda"])], loose: ["packages/core/override/whistle.uti"]);
        var hashes = Hashes(("AddIns/Extra_Dog_Slot/core/dog.gda", "dog"), ("packages/core/override/whistle.uti", "whistle"));

        var match = MatchOnly(snapshot, [_dogSlot], hashes, "addin:Extra_Dog_Slot");

        Assert.Equal(("extra-dog-slot-1", "Extra Dog Slot", 0), (match!.ModId, match.ModName, match.DifferingFiles));
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["Contents/addins/Extra_Dog_Slot/core/dog.gda"] = "AddIns/Extra_Dog_Slot/core/dog.gda",
                ["Contents/packages/core/override/whistle.uti"] = "packages/core/override/whistle.uti",
            },
            match.Sources);
    }

    [Fact]
    public void Match_AddInWithAChangedFile_DiffersByThatFile()
    {
        var snapshot = Snapshot(addIns: [("Extra_Dog_Slot", ["AddIns/Extra_Dog_Slot/core/dog.gda"])], loose: ["packages/core/override/whistle.uti"]);
        var hashes = Hashes(("AddIns/Extra_Dog_Slot/core/dog.gda", "dog"), ("packages/core/override/whistle.uti", "edited"));

        Assert.Equal(1, MatchOnly(snapshot, [_dogSlot], hashes, "addin:Extra_Dog_Slot")!.DifferingFiles);
    }

    [Fact]
    public void Match_OverrideFolderByNameWithAnExtraFile_DiffersByTheExtraFile()
    {
        var snapshot = Snapshot(overrides: [("Better Hair", ["packages/core/override/Better Hair/hair.dds", "packages/core/override/Better Hair/brow.dds", "packages/core/override/Better Hair/mine.dds"])]);
        var hashes = Hashes(("packages/core/override/Better Hair/hair.dds", "hair"), ("packages/core/override/Better Hair/brow.dds", "brow"));

        var match = MatchOnly(snapshot, [_hair], hashes, "override:Better Hair")!;

        Assert.Equal(("better-hair-1", "Better Hair", 1), (match.ModId, match.ModName, match.DifferingFiles));
    }

    [Fact]
    public void Match_NoFileInCommon_IsNoMatch()
    {
        var snapshot = Snapshot(overrides: [("Better Hair", ["packages/core/override/Better Hair/hair.dds"])]);

        Assert.Null(MatchOnly(snapshot, [_hair], Hashes(("packages/core/override/Better Hair/hair.dds", "someone else's")), "override:Better Hair"));
    }

    [Fact]
    public void PathsToHash_OnlyFilesWhereALibraryFileWouldBeWithItsSize()
    {
        var snapshot = Snapshot(
            addIns: [("Extra_Dog_Slot", ["AddIns/Extra_Dog_Slot/core/dog.gda"])],
            overrides: [("Better Hair", ["packages/core/override/Better Hair/hair.dds"]), ("Other", ["packages/core/override/Other/other.dds"])]);
        snapshot = snapshot with { OverrideFolders = new Dictionary<string, IReadOnlyList<HandInstalledFile>>(snapshot.OverrideFolders) { ["Big"] = [new HandInstalledFile("packages/core/override/Big/brow.dds", 999)] } };
        var scan = AdoptionScanner.Scan(snapshot, DeploymentRecord.Empty, new KnownItemsCatalog(1, [], []));

        var paths = AdoptionLibraryMatcher.PathsToHash(snapshot, scan, [_dogSlot, _hair]);

        Assert.Equal(["AddIns/Extra_Dog_Slot/core/dog.gda", "packages/core/override/Better Hair/hair.dds"], paths);
    }

    private static LibraryModFile File(string path, string content) => new(path, content.Length, content);

    private static Dictionary<string, string> Hashes(params (string Path, string Content)[] files) =>
        files.ToDictionary(f => f.Path, f => f.Content, StringComparer.OrdinalIgnoreCase);

    private static LibraryMatch? MatchOnly(HandInstalledSnapshot snapshot, IReadOnlyList<LibraryMod> library, Dictionary<string, string> hashes, string id)
    {
        var scan = AdoptionScanner.Scan(snapshot, DeploymentRecord.Empty, new KnownItemsCatalog(1, [], []));
        return AdoptionLibraryMatcher.Match(snapshot, scan, library, hashes).Candidates.Single(c => c.Id == id).LibraryMatch;
    }

    /// <summary>Each file's size is the length of its name's last segment without extension, matching <see cref="File"/>'s content.</summary>
    private static HandInstalledSnapshot Snapshot(
        (string Name, string[] Files)[]? addIns = null,
        (string Name, string[] Files)[]? overrides = null,
        string[]? loose = null) =>
        new(
            (addIns ?? []).Select(a => new RegisteredAddIn(a.Name, a.Name)).ToList(),
            Folders(addIns ?? []),
            Folders(overrides ?? []),
            (loose ?? []).Select(Disk).ToList());

    private static Dictionary<string, IReadOnlyList<HandInstalledFile>> Folders((string Name, string[] Files)[] folders) =>
        folders.ToDictionary(f => f.Name, f => (IReadOnlyList<HandInstalledFile>)f.Files.Select(Disk).ToList(), StringComparer.OrdinalIgnoreCase);

    private static HandInstalledFile Disk(string path) => new(path, Path.GetFileNameWithoutExtension(path).Length);
}
