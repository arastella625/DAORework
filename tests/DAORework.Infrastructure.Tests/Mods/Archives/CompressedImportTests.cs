using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods.Archives;

/// <summary>A <c>.7z</c> or <c>.rar</c> is imported like a <c>.zip</c>, and refused the same way when it is malformed or malicious (P2.5-04).</summary>
public sealed class CompressedImportTests
{
    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Downloads");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FolderModLibrary _library;

    public CompressedImportTests()
    {
        _library = new FolderModLibrary(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }), new FakeTimeProvider());
    }

    private string Library => Path.Combine(_appData, "library");

    [Fact]
    public async Task Import_Rar_AddIn_IsImportedWithItsManifest()
    {
        var entry = (await ImportAsync("Dog.rar", TestArchives.Rar(Manifest("Dog_UID", "Dog"), ("Contents\\addins\\Dog_UID\\dog.gda", "dog")))).Value.Entry;

        Assert.Equal(ModKind.AddIn, entry.Mod.Kind);
        Assert.Equal("Dog_UID", entry.Mod.AddInUid);
        Assert.Equal(["Contents/addins/Dog_UID/dog.gda", "Manifest.xml"], entry.Mod.Files.Select(f => f.RelativePath));
        Assert.Equal("dog", _fileSystem.File.ReadAllText(Path.Combine(Library, entry.Mod.Id, "Contents", "addins", "Dog_UID", "dog.gda")));
    }

    [Fact]
    public async Task Import_SevenZip_OverrideMod_IsReadFrontToBackWithEveryFile()
    {
        var entry = (await ImportAsync("Hair Pack.7z", TestArchives.SevenZip(("textures/hair.dds", "DDS hair"), ("hair.mmh", "mesh"), ("TEXTURES/HAIR.dds", "a duplicate")))).Value.Entry;

        Assert.Equal(ModKind.Override, entry.Mod.Kind);
        Assert.Equal(["hair.mmh", "textures/hair.dds"], entry.Mod.Files.Select(f => f.RelativePath));
        Assert.Equal("DDS hair", _fileSystem.File.ReadAllText(Path.Combine(Library, entry.Mod.Id, "textures", "hair.dds")));
    }

    [Fact]
    public async Task Import_SevenZip_HoldingADazip_IsUnwrapped()
    {
        var dazip = TestArchives.Zip(Manifest("Inner_UID", "Inner"), ("Contents/addins/Inner_UID/a.gda", "a"));

        var entry = (await ImportAsync("Inner.7z", TestArchives.SevenZip(("readme.txt", "hi"u8.ToArray()), ("Inner.dazip", dazip)))).Value.Entry;

        Assert.Equal("Inner_UID", entry.Mod.AddInUid);
        Assert.Equal("Inner.dazip", entry.SourceName);
    }

    [Theory]
    [InlineData("Evil.rar")]
    [InlineData("Evil.7z")]
    public async Task Import_PathLeavingTheModFolder_IsRefusedAndNothingIsWritten(string name)
    {
        (string, string)[] files = [("hair.dds", "DDS"), ("../../escaped.dds", "evil")];
        var bytes = name.EndsWith(".rar", StringComparison.Ordinal) ? TestArchives.Rar(files) : TestArchives.SevenZip(files);

        var result = await ImportAsync(name, bytes);

        Assert.Equal(DiagnosticCodes.ModImportInvalid, result.Error!.Code);
        Assert.Contains("would be written outside the mod's folder", result.Error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(_fileSystem.AllFiles, f => f.EndsWith("escaped.dds", StringComparison.Ordinal));
        Assert.False(_fileSystem.Directory.Exists(Library));
    }

    public static TheoryData<string, byte[]> Damaged()
    {
        var rar = TestArchives.Rar(("hair.dds", "DDS hair"), ("eyes.dds", "DDS eyes"));
        var flipped = (byte[])rar.Clone();
        flipped[Array.IndexOf(flipped, (byte)'D')] ^= 1;
        var sevenZip = TestArchives.SevenZip(("hair.dds", "DDS hair"));
        return new()
        {
            { "Truncated.rar", rar[..^12] },
            { "Half.rar", rar[..(rar.Length / 2)] },
            { "Crc.rar", flipped },
            { "Text.rar", "this is not an archive"u8.ToArray() },
            { "Truncated.7z", sevenZip[..^10] },
        };
    }

    [Theory]
    [MemberData(nameof(Damaged))]
    public async Task Import_DamagedArchive_IsRefusedWithAHintAndNotAdded(string name, byte[] bytes)
    {
        var result = await ImportAsync(name, bytes);

        Assert.Equal(DiagnosticCodes.ModImportInvalid, result.Error!.Code);
        Assert.Contains("could not be read as an archive", result.Error.Message, StringComparison.Ordinal);
        Assert.Contains("Download it again", result.Error.Recommendation, StringComparison.Ordinal);
        Assert.Empty(await _library.ListAsync());
    }

    [Fact]
    public async Task Import_ZipNamedSevenZip_IsRefusedForWhatItIs()
    {
        var result = await ImportAsync("Renamed.7z", TestArchives.Zip(("hair.dds", "DDS")));

        Assert.Equal(DiagnosticCodes.ModImportFailed, result.Error!.Code);
        Assert.Contains("it is a Zip archive, not a .7z or .rar.", result.Error.Message, StringComparison.Ordinal);
    }

    private static (string Name, string Content) Manifest(string uid, string name) =>
        ("Manifest.xml", $"<Manifest><AddInsList><AddInItem UID=\"{uid}\" Name=\"{name}\" /></AddInsList></Manifest>");

    private Task<Result<LibraryImport>> ImportAsync(string name, byte[] bytes)
    {
        var path = Path.Combine(_downloads, name);
        _fileSystem.AddFile(path, new MockFileData(bytes));
        return _library.ImportAsync(path);
    }
}
