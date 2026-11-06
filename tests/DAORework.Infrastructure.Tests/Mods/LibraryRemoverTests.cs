using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class LibraryRemoverTests
{
    private static readonly string _appData = MockUnixSupport.Path(@"C:\data");
    private static readonly string _root = Path.Combine(_appData, "library");

    private readonly MockFileSystem _fileSystem = new();

    private LibraryRemover Remover => new(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }));

    [Fact]
    public async Task RemoveAsync_TakesTheMod_AndLeavesTheOthers()
    {
        Seed("hair-aaaaaaaa");
        Seed("eyes-bbbbbbbb");

        var result = await Remover.RemoveAsync("hair-aaaaaaaa");

        Assert.True(result.IsSuccess);
        Assert.False(_fileSystem.File.Exists(Path.Combine(_root, "hair-aaaaaaaa.mod.json")));
        Assert.False(_fileSystem.Directory.Exists(Path.Combine(_root, "hair-aaaaaaaa")));
        Assert.True(_fileSystem.File.Exists(Path.Combine(_root, "eyes-bbbbbbbb", "hair.dds")));
    }

    [Fact]
    public async Task RemoveAsync_AnIdNotInTheLibrary_RemovesNothing()
    {
        _fileSystem.AddFile(Path.Combine(_root, "orphan", "hair.dds"), new MockFileData("x"));

        var result = await Remover.RemoveAsync("orphan");

        Assert.Equal(DiagnosticCodes.ModNotInLibrary, result.Error!.Code);
        Assert.True(_fileSystem.Directory.Exists(Path.Combine(_root, "orphan")));
    }

    [Fact]
    public async Task RemoveAsync_AnIdThatLeavesTheLibrary_IsRefused()
    {
        var outside = Path.Combine(_appData, "keep.mod.json");
        _fileSystem.AddFile(outside, new MockFileData("{}"));

        var result = await Remover.RemoveAsync("../keep");

        Assert.Equal(DiagnosticCodes.ModNotInLibrary, result.Error!.Code);
        Assert.True(_fileSystem.File.Exists(outside));
    }

    private void Seed(string id)
    {
        _fileSystem.AddFile(Path.Combine(_root, id + ".mod.json"), new MockFileData("{}"));
        _fileSystem.AddFile(Path.Combine(_root, id, "hair.dds"), new MockFileData("x"));
    }
}
