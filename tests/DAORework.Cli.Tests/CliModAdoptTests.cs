using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Cli.Composition;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>mods adopt --dry-run (ADR-0015, ROADMAP P2.5-02) end to end, over a documents folder in memory.</summary>
public sealed class CliModAdoptTests : IDisposable
{
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");

    private readonly CliTestHost _host = new();

    public CliModAdoptTests()
    {
        _host.FileSystem.AddFile(
            Path.Combine(_documents, "Settings", "AddIns.xml"),
            new MockFileData("<AddInsList><AddInItem UID=\"Extra_Dog_Slot\"><Title DefaultText=\"Extra Dog Slot v1.41\" /></AddInItem><AddInItem UID=\"DAO_PRC_EP_1\" /></AddInsList>"));
        _host.FileSystem.AddFile(Path.Combine(_documents, "AddIns", "Extra_Dog_Slot", "dog.gda"), new MockFileData("dog"));
        _host.FileSystem.AddFile(Path.Combine(_documents, "packages", "core", "override", "Better Hair", "hair.dds"), new MockFileData("hair"));
        _host.Run("init", "Mods");
        _host.Run("profile", "set-path", "Mods", "documents", _documents);
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Adopt_DryRun_ListsCandidatesWithTheCatalogAndLeavesOutOfficialDlc()
    {
        var result = _host.Run("mods", "adopt", "--dry-run", "--files", "-p", "Mods");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("Installed by hand, can be adopted (2):", result.Output, StringComparison.Ordinal);
        Assert.Contains("addin:Extra_Dog_Slot  Extra Dog Slot v1.41  (add-in, 1 file(s)", result.Output, StringComparison.Ordinal);
        Assert.Contains("Known as Extra Dog Slot: https://www.nexusmods.com/dragonage/mods/664", result.Output, StringComparison.Ordinal);
        Assert.Contains("override:Better Hair  Better Hair  (override folder", result.Output, StringComparison.Ordinal);
        Assert.Contains("packages/core/override/Better Hair/hair.dds", result.Output, StringComparison.Ordinal);
        Assert.Contains("Official DLC (1 add-in(s)) is never adopted or changed.", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("addin:DAO_PRC_EP_1", result.Output, StringComparison.Ordinal);
        Assert.Contains("Nothing was changed. To adopt: mods adopt <id>...", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Adopt_DryRun_AddInLikeAnImportedDownload_IsTheLibraryMod()
    {
        var download = MockUnixSupport.Path(@"C:\Downloads\dog.dazip");
        _host.FileSystem.AddFile(download, new MockFileData(Dazip()));
        _host.Run("mods", "import", download);

        var result = _host.Run("mods", "adopt", "--dry-run", "-p", "Mods");

        Assert.Contains("Same as library mod Extra Dog Slot (extra-dog-slot-", result.Output, StringComparison.Ordinal);
        Assert.Contains("adopting imports nothing new.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Adopt_DryRun_FolderWithAProgram_SaysAdoptingLeavesItWhereItIs()
    {
        _host.FileSystem.AddFile(Path.Combine(_documents, "packages", "core", "override", "Better Hair", "install.bat"), new MockFileData("@echo off"));

        var result = _host.Run("mods", "adopt", "--dry-run", "-p", "Mods");

        Assert.Contains("Holds 1 program file(s) (install.bat): adopting leaves them where they are, and DAORework never installs programs.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Adopt_OverrideFolder_MovesItIntoDaoreworksFolderAndRollbackPutsItBack()
    {
        var original = Path.Combine(_documents, "packages", "core", "override", "Better Hair", "hair.dds");

        var adopt = _host.Run("mods", "adopt", "override:Better Hair", "-p", "Mods", "--yes");
        var moved = Directory(Path.Combine(_documents, "packages", "core", "override", "DAORework")).Single();
        var status = _host.Run("mods", "status", "-p", "Mods");
        var rollback = _host.Run("mods", "rollback", "-p", "Mods", "--yes");

        Assert.Equal(ExitCodes.Success, adopt.ExitCode);
        Assert.Contains("Move      1 file", adopt.Output, StringComparison.Ordinal);
        Assert.EndsWith(Path.Combine("hair.dds"), moved, StringComparison.Ordinal);
        Assert.Contains("better-hair-", status.Output, StringComparison.Ordinal);
        Assert.Equal(ExitCodes.Success, rollback.ExitCode);
        Assert.Equal("hair", _host.FileSystem.File.ReadAllText(original));
        Assert.Empty(Directory(Path.Combine(_documents, "packages", "core", "override", "DAORework")));
    }

    [Fact]
    public void Adopt_AddInInPlace_RecordsItAndLeavesAddInsXmlAsItWas()
    {
        var list = Path.Combine(_documents, "Settings", "AddIns.xml");
        var before = _host.FileSystem.File.ReadAllText(list);

        var adopt = _host.Run("mods", "adopt", "addin:Extra_Dog_Slot", "-p", "Mods", "--yes");
        var status = _host.Run("mods", "status", "-p", "Mods");

        Assert.Equal(ExitCodes.Success, adopt.ExitCode);
        Assert.Contains("Adopt     1 file where they are", adopt.Output, StringComparison.Ordinal);
        Assert.Equal("dog", _host.FileSystem.File.ReadAllText(Path.Combine(_documents, "AddIns", "Extra_Dog_Slot", "dog.gda")));
        Assert.Equal(before, _host.FileSystem.File.ReadAllText(list));
        Assert.Contains("extra-dog-slot-", status.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("addin:Extra_Dog_Slot", _host.Run("mods", "adopt", "--dry-run", "-p", "Mods").Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("addin:DAO_PRC_EP_1", "addin:DAO_PRC_EP_1 cannot be adopted: official DLC.")]
    [InlineData("override:Nothing Here", "override:Nothing Here is not something installed by hand in this documents folder.")]
    public void Adopt_IdThatIsNotACandidate_FailsWithTheReason(string id, string message)
    {
        var result = _host.Run("mods", "adopt", id, "-p", "Mods", "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains(message, result.Output, StringComparison.Ordinal);
        Assert.Contains("Run mods adopt --dry-run for the ids.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Adopt_LooseFilesWithoutAName_AsksForOne()
    {
        _host.FileSystem.AddFile(Path.Combine(_documents, "packages", "core", "override", "loose.uti"), new MockFileData("l"));

        var result = _host.Run("mods", "adopt", "override:(loose)", "-p", "Mods", "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("--name", result.Output, StringComparison.Ordinal);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(_documents, "packages", "core", "override", "loose.uti")));
    }

    [Fact]
    public void Adopt_DryRunWithIds_ShowsThePlanAndMovesNothing()
    {
        var result = _host.Run("mods", "adopt", "override:Better Hair", "-p", "Mods", "--dry-run", "--files");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("> packages/core/override/Better Hair/hair.dds -> packages/core/override/DAORework/better-hair-", result.Output, StringComparison.Ordinal);
        Assert.Contains("Dry run: nothing was changed.", result.Output, StringComparison.Ordinal);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(_documents, "packages", "core", "override", "Better Hair", "hair.dds")));
    }

    private IEnumerable<string> Directory(string path) =>
        _host.FileSystem.Directory.Exists(path) ? _host.FileSystem.Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories) : [];

    private static byte[] Dazip()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var writer = new StreamWriter(zip.CreateEntry("Manifest.xml").Open()))
            {
                writer.Write("<Manifest><AddInsList><AddInItem UID=\"Extra_Dog_Slot\" Name=\"Extra Dog Slot\" /></AddInsList></Manifest>");
            }

            using (var writer = new StreamWriter(zip.CreateEntry("Contents/addins/Extra_Dog_Slot/dog.gda").Open()))
            {
                writer.Write("dog");
            }
        }

        return buffer.ToArray();
    }
}
