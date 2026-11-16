using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>P2.8-04 (ADR-0033 §2): a library file read as it is now, before a program goes into the game folder.</summary>
public sealed class LibraryFileReaderTests
{
    private static readonly string _appData = MockUnixSupport.Path(@"C:\AppData\DAORework");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task ReadAsync_AFileThere_GivesItsPathHashAndFirstBytes()
    {
        byte[] bytes = [.. "MZ"u8, 1, 2, 3, 4];
        var path = Path.Combine(_appData, "library", "luma-0a1b2c3d", "Luma", "d3d9.dll");
        _fileSystem.AddFile(path, new MockFileData(bytes));

        var read = (await Reader().ReadAsync("luma-0a1b2c3d", new LibraryModFile("Luma/d3d9.dll", 6, "x"), startBytes: 2)).Value;

        Assert.Equal(path, read.Path);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), read.Sha256);
        Assert.Equal("MZ"u8.ToArray(), read.Start);
    }

    [Fact]
    public async Task ReadAsync_AMissingFile_FailsSayingToAddTheModAgain()
    {
        var error = (await Reader().ReadAsync("luma-0a1b2c3d", new LibraryModFile("d3d9.dll", 6, "x"), 2)).Error!;

        Assert.Equal(DiagnosticCodes.ModNotInLibrary, error.Code);
        Assert.Contains("Add the mod again", error.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsync_APathOutOfTheLibrary_IsNeverRead()
    {
        var error = (await Reader().ReadAsync("luma-0a1b2c3d", new LibraryModFile("../../profiles/default.json", 6, "x"), 2)).Error!;

        Assert.Equal(DiagnosticCodes.ModNotInLibrary, error.Code);
    }

    private LibraryFileReader Reader() => new(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }));
}
