using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Text;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class FolderModLibraryTests
{
    private const string Manifest = "<Manifest Type=\"AddIn\"><AddInsList><AddInItem UID=\"Extra_Dog_Slot\" Name=\"Extra Dog Slot\" /></AddInsList></Manifest>";

    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");
    private static readonly DateTimeOffset _entryTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly MockFileSystem _fileSystem = new();
    private readonly FolderModLibrary _library;

    public FolderModLibraryTests()
    {
        _library = new FolderModLibrary(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }), new FakeTimeProvider(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Import_Dazip_IsAnAddInWithEveryFileHashedAndCopied()
    {
        var path = Download("dog.dazip", Dazip());

        var import = (await _library.ImportAsync(path)).Value;

        var mod = import.Entry.Mod;
        Assert.False(import.AlreadyInLibrary);
        Assert.Equal(ModKind.AddIn, mod.Kind);
        Assert.Equal("Extra_Dog_Slot", mod.AddInUid);
        Assert.Equal("Extra Dog Slot", mod.Name);
        Assert.StartsWith("extra-dog-slot-", mod.Id, StringComparison.Ordinal);
        Assert.Equal(["Contents/addins/Extra_Dog_Slot/dog.gda", "Manifest.xml"], mod.Files.Select(f => f.RelativePath));
        Assert.Equal("dog", Library(mod.Id, "Contents/addins/Extra_Dog_Slot/dog.gda"));
        Assert.Equal("dog.dazip", import.Entry.SourceName);
        Assert.True(_fileSystem.File.Exists(Path.Combine(_appData, "library", mod.Id + ".mod.json")));
        Assert.False(_fileSystem.Directory.Exists(Path.Combine(_appData, "library", mod.Id + ".tmp")));
    }

    [Fact]
    public async Task Import_NexusZipWithADazipInside_IsTheSameEntryAsTheDazip()
    {
        var direct = (await _library.ImportAsync(Download("dog.dazip", Dazip()))).Value;

        var wrapped = (await _library.ImportAsync(Download("Extra Dog Slot-664-1-41.zip", Zip(("dao_extradogslot.dazip", Dazip()), ("readme.txt", "hi"u8.ToArray()))))).Value;

        Assert.True(wrapped.AlreadyInLibrary);
        Assert.Equal(direct.Entry.Mod.Id, wrapped.Entry.Mod.Id);
    }

    [Fact]
    public async Task Import_OverrideZip_KeepsItsLayoutAndIsNamedWithoutTheNexusSuffix()
    {
        var path = Download("Better Hair-1234-1-0-1600000000.zip", Zip(("Better Hair/hair.dds", "hair"u8.ToArray()), (@"Better Hair\eyes.dds", "eyes"u8.ToArray())));

        var mod = (await _library.ImportAsync(path)).Value.Entry.Mod;

        Assert.Equal(ModKind.Override, mod.Kind);
        Assert.Equal("Better Hair", mod.Name);
        Assert.Null(mod.AddInUid);
        Assert.Equal(["Better Hair/eyes.dds", "Better Hair/hair.dds"], mod.Files.Select(f => f.RelativePath));
        Assert.Equal("hair", Library(mod.Id, "Better Hair/hair.dds"));
    }

    [Fact]
    public async Task Import_Folder_IsAnOverrideModWhoseIdFollowsItsContent()
    {
        var folder = Path.Combine(_downloads, "Loose Mod");
        _fileSystem.AddFile(Path.Combine(folder, "textures", "a.dds"), new MockFileData("tex"));

        var first = (await _library.ImportAsync(folder)).Value;
        _fileSystem.File.WriteAllText(Path.Combine(folder, "textures", "a.dds"), "changed");
        var second = (await _library.ImportAsync(folder)).Value;

        Assert.Equal("Loose Mod", first.Entry.Mod.Name);
        Assert.Equal(["textures/a.dds"], first.Entry.Mod.Files.Select(f => f.RelativePath));
        Assert.NotEqual(first.Entry.Mod.Id, second.Entry.Mod.Id);
        Assert.Equal(2, (await _library.ListAsync()).Count);
    }

    [Fact]
    public async Task Import_ZipSlip_IsRefusedAndWritesNothing()
    {
        var result = await _library.ImportAsync(Download("evil.zip", Zip(("../../evil.txt", "x"u8.ToArray()))));

        Assert.Equal(DiagnosticCodes.ModImportInvalid, result.Error!.Code);
        Assert.False(_fileSystem.Directory.Exists(Path.Combine(_appData, "library")));
    }

    [Theory]
    [InlineData("mod.tar", DiagnosticCodes.ModImportUnsupported)]
    [InlineData("missing.zip", DiagnosticCodes.ModImportFailed)]
    public async Task Import_NotAnArchiveItCanRead_Fails(string name, string code)
    {
        if (name == "mod.tar")
        {
            Download(name, [0x37, 0x7A]);
        }

        Assert.Equal(code, (await _library.ImportAsync(Path.Combine(_downloads, name))).Error!.Code);
    }

    [Fact]
    public async Task Import_CorruptZip_FailsInsteadOfThrowing()
    {
        Assert.Equal(DiagnosticCodes.ModImportFailed, (await _library.ImportAsync(Download("broken.zip", "not a zip"u8.ToArray()))).Error!.Code);
    }

    [Fact]
    public async Task Import_DazipWithoutAUsableManifest_Fails()
    {
        var path = Download("bad.dazip", Zip(("Manifest.xml", "<Manifest />"u8.ToArray()), ("Contents/addins/x/a.gda", "a"u8.ToArray())));

        Assert.Equal(DiagnosticCodes.ModImportInvalid, (await _library.ImportAsync(path)).Error!.Code);
    }

    [Fact]
    public async Task Import_AfterAnImportThatNeverFinished_StartsOver()
    {
        var path = Download("dog.dazip", Dazip());
        var id = (await _library.ImportAsync(path)).Value.Entry.Mod.Id;
        _fileSystem.File.WriteAllText(Path.Combine(_appData, "library", id + ".mod.json"), "{ cut short");
        _fileSystem.AddFile(Path.Combine(_appData, "library", id + ".tmp", "stale.txt"), new MockFileData("stale"));
        _fileSystem.AddFile(Path.Combine(_appData, "library", id, "stale.txt"), new MockFileData("stale"));

        var again = (await _library.ImportAsync(path)).Value;

        Assert.False(again.AlreadyInLibrary);
        Assert.False(_fileSystem.File.Exists(Path.Combine(_appData, "library", id, "stale.txt")));
        Assert.Single(await _library.ListAsync());
    }

    [Fact]
    public async Task List_IsSortedByNameAndEmptyBeforeAnyImport()
    {
        Assert.Empty(await _library.ListAsync());
        await _library.ImportAsync(Download("zeta.zip", Zip(("z.dds", "z"u8.ToArray()))));
        await _library.ImportAsync(Download("Alpha.zip", Zip(("a.dds", "a"u8.ToArray()))));

        Assert.Equal(["Alpha", "zeta"], (await _library.ListAsync()).Select(e => e.Mod.Name));
    }

    private static byte[] Dazip() => Zip(("Manifest.xml", Encoding.UTF8.GetBytes(Manifest)), ("Contents/addins/Extra_Dog_Slot/dog.gda", "dog"u8.ToArray()));

    private static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                // A fixed time, so the same entries always make the same bytes: ZipArchive stamps entries with the current time otherwise.
                var entry = zip.CreateEntry(name);
                entry.LastWriteTime = _entryTime;
                using var stream = entry.Open();
                stream.Write(content);
            }
        }

        return buffer.ToArray();
    }

    private string Download(string name, byte[] content)
    {
        var path = Path.Combine(_downloads, name);
        _fileSystem.AddFile(path, new MockFileData(content));
        return path;
    }

    private string Library(string id, string relative) =>
        _fileSystem.File.ReadAllText(Path.Combine(_appData, "library", id, relative.Replace('/', Path.DirectorySeparatorChar)));
}
