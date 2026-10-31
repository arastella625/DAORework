using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;
using DAORework.Infrastructure.Fixes;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Fixes;

public sealed class FolderFixLibraryTests
{
    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    private static readonly byte[] _patched = "patched"u8.ToArray();
    private static readonly byte[] _original = "original"u8.ToArray();

    private readonly MockFileSystem _fileSystem = new();
    private readonly FolderFixLibrary _library;

    public FolderFixLibraryTests()
    {
        _library = new FolderFixLibrary(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }));
    }

    [Fact]
    public async Task Add_StoresEveryFileAndTheEntryListsBack()
    {
        var entry = Entry();

        var added = (await _library.AddAsync(entry, Contents())).Value;

        Assert.Equal(entry.Id, added.Id);
        Assert.Equal(_original, _fileSystem.File.ReadAllBytes(Stored(entry.Id, "original/daorigins.exe")));
        Assert.Equal(_patched, _fileSystem.File.ReadAllBytes(Stored(entry.Id, "patched/daorigins.exe")));
        var listed = Assert.Single(await _library.ListAsync());
        Assert.Equal((entry.Id, "bin_ship/daorigins.exe", "original/daorigins.exe"), (listed.Id, listed.Files[0].TargetPath, listed.Kept[0].RelativePath));
        Assert.False(_fileSystem.Directory.Exists(Stored(entry.Id + ".tmp", string.Empty)));
    }

    [Fact]
    public async Task Add_AnEntryAlreadyThere_ReturnsItWithoutWriting()
    {
        await _library.AddAsync(Entry(), Contents());
        _fileSystem.File.WriteAllText(Stored(Entry().Id, "patched/daorigins.exe"), "untouched");

        await _library.AddAsync(Entry(), Contents());

        Assert.Equal("untouched", _fileSystem.File.ReadAllText(Stored(Entry().Id, "patched/daorigins.exe")));
    }

    [Fact]
    public async Task Add_ContentThatDoesNotMatchItsHash_StoresNothing()
    {
        var result = await _library.AddAsync(Entry(), new Dictionary<string, byte[]> { ["patched/daorigins.exe"] = _original, ["original/daorigins.exe"] = _original });

        Assert.Equal(DiagnosticCodes.FixLibraryFailed, result.Error!.Code);
        Assert.Empty(await _library.ListAsync());
    }

    [Fact]
    public async Task Add_AfterAnAddThatNeverFinished_StartsOver()
    {
        _fileSystem.AddFile(Stored(Entry().Id, "stale.txt"), new MockFileData("stale"));
        _fileSystem.AddFile(Stored(Entry().Id, FolderFixLibrary.MetadataFileName), new MockFileData("{ cut short"));

        await _library.AddAsync(Entry(), Contents());

        Assert.False(_fileSystem.File.Exists(Stored(Entry().Id, "stale.txt")));
        Assert.Single(await _library.ListAsync());
    }

    [Fact]
    public async Task List_BeforeAnyFix_IsEmpty()
    {
        Assert.Empty(await _library.ListAsync());
    }

    private static FixEntry Entry() => new(
        "large-address-aware-12345678",
        FixIds.LargeAddressAware,
        null,
        [new FixFile("bin_ship/daorigins.exe", new LibraryModFile("patched/daorigins.exe", _patched.Length, Sha(_patched)))],
        [new LibraryModFile("original/daorigins.exe", _original.Length, Sha(_original))]);

    private static Dictionary<string, byte[]> Contents() => new() { ["patched/daorigins.exe"] = _patched, ["original/daorigins.exe"] = _original };

    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static string Stored(string id, string relative) =>
        Path.Combine(_appData, "library", "fixes", id, relative.Replace('/', Path.DirectorySeparatorChar)).TrimEnd(Path.DirectorySeparatorChar);
}
