using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Cli.Composition;
using Xunit;

namespace DAORework.Cli.Tests;

public sealed class CliModLibraryTests : IDisposable
{
    private static readonly string _download = MockUnixSupport.Path(@"C:\Downloads\Extra Dog Slot.dazip");

    private readonly CliTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Import_ThenLibrary_ShowsTheModAndSaysNothingWasInstalled()
    {
        _host.FileSystem.AddFile(_download, new MockFileData(Dazip()));

        var import = _host.Run("mods", "import", _download);
        var again = _host.Run("mods", "import", _download);
        var library = _host.Run("mods", "library");

        Assert.Equal(ExitCodes.Success, import.ExitCode);
        Assert.Contains("Imported: Extra Dog Slot (add-in Extra_Dog_Slot) as extra-dog-slot-", import.Output, StringComparison.Ordinal);
        Assert.Contains("Nothing was installed", import.Output, StringComparison.Ordinal);
        Assert.Contains("Not scanned: The Windows antimalware scan (AMSI) is only on Windows.", import.Output, StringComparison.Ordinal);
        Assert.Contains(
            "[Warning] DAO_MOD_SAFETY_DOWNLOAD_DIFFERS: Extra Dog Slot.dazip differs from the known downloads of Extra Dog Slot. Unless it is a newer version, get it from https://www.nexusmods.com/dragonage/mods/664",
            import.Output,
            StringComparison.Ordinal);
        Assert.Contains("Already in the library", again.Output, StringComparison.Ordinal);
        Assert.Contains("Extra Dog Slot  (add-in Extra_Dog_Slot, 2 file(s)", library.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_UnsupportedFile_FailsWithTheHint()
    {
        var path = MockUnixSupport.Path(@"C:\Downloads\mod.tar");
        _host.FileSystem.AddFile(path, new MockFileData("tar"));

        var result = _host.Run("mods", "import", path);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("is not a .dazip, .zip, .7z or .rar, or a folder.", result.Output, StringComparison.Ordinal);
        Assert.Contains("Extract it with an archive tool such as 7-Zip", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_ModWithAProgram_NamesItAndSaysItIsNeverInstalled()
    {
        var path = MockUnixSupport.Path(@"C:\Downloads\Hair Pack.zip");
        _host.FileSystem.AddFile(path, new MockFileData(Zip(("hair.dds", "hair"), ("tools/setup.exe", "MZ"))));

        var import = _host.Run("mods", "import", path);
        var library = _host.Run("mods", "library");

        Assert.Contains("It holds 1 program file(s) (tools/setup.exe). They stay in the library and are never installed into the game's folders.", import.Output, StringComparison.Ordinal);
        Assert.Contains("2 file(s), 6 B, 1 program file(s) never installed", library.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_Empty_SaysHowToImport()
    {
        Assert.Contains("The mod library is empty. Run: mods import", _host.Run("mods", "library").Output, StringComparison.Ordinal);
    }

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

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }
}
