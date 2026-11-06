using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Abstractions;
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

    private readonly NoteTaker _bin = new();

    private LibraryRemover Remover => new(_fileSystem, Options.Create(new DaoReworkOptions { AppDataDirectory = _appData }), _bin);

    [Fact]
    public void Remove_TakesTheMod_AndLeavesTheOthers()
    {
        Seed("hair-aaaaaaaa");
        Seed("eyes-bbbbbbbb");

        var result = Remover.Remove("hair-aaaaaaaa");

        Assert.True(result.IsSuccess);
        Assert.False(_fileSystem.File.Exists(Path.Combine(_root, "hair-aaaaaaaa.mod.json")));
        Assert.False(_fileSystem.Directory.Exists(Path.Combine(_root, "hair-aaaaaaaa")));
        Assert.True(_fileSystem.File.Exists(Path.Combine(_root, "eyes-bbbbbbbb", "hair.dds")));
    }

    [Fact]
    public void Remove_WithARecycleBin_SendsTheRecordFirst_AndDeletesNothingItself()
    {
        Seed("hair-aaaaaaaa");
        _bin.IsAvailable = true;

        var result = Remover.Remove("hair-aaaaaaaa");

        Assert.True(result.IsSuccess);
        Assert.Equal([Path.Combine(_root, "hair-aaaaaaaa.mod.json"), Path.Combine(_root, "hair-aaaaaaaa")], _bin.Sent);
        Assert.True(_fileSystem.File.Exists(Path.Combine(_root, "hair-aaaaaaaa", "hair.dds")));
    }

    [Fact]
    public void Remove_ARecycleBinThatFails_SaysSo_AndTheModStays()
    {
        Seed("hair-aaaaaaaa");
        _bin.IsAvailable = true;
        _bin.Fails = true;

        var result = Remover.Remove("hair-aaaaaaaa");

        Assert.Equal(DiagnosticCodes.ModRemoveFailed, result.Error!.Code);
        Assert.True(_fileSystem.File.Exists(Path.Combine(_root, "hair-aaaaaaaa.mod.json")));
    }

    [Fact]
    public void Remove_AnIdNotInTheLibrary_RemovesNothing()
    {
        _fileSystem.AddFile(Path.Combine(_root, "orphan", "hair.dds"), new MockFileData("x"));

        var result = Remover.Remove("orphan");

        Assert.Equal(DiagnosticCodes.ModNotInLibrary, result.Error!.Code);
        Assert.True(_fileSystem.Directory.Exists(Path.Combine(_root, "orphan")));
    }

    [Fact]
    public void Remove_AnIdThatLeavesTheLibrary_IsRefused()
    {
        var outside = Path.Combine(_appData, "keep.mod.json");
        _fileSystem.AddFile(outside, new MockFileData("{}"));

        var result = Remover.Remove("../keep");

        Assert.Equal(DiagnosticCodes.ModNotInLibrary, result.Error!.Code);
        Assert.True(_fileSystem.File.Exists(outside));
    }

    private void Seed(string id)
    {
        _fileSystem.AddFile(Path.Combine(_root, id + ".mod.json"), new MockFileData("{}"));
        _fileSystem.AddFile(Path.Combine(_root, id, "hair.dds"), new MockFileData("x"));
    }

    private sealed class NoteTaker : IRecycleBin
    {
        public bool IsAvailable { get; set; }

        public bool Fails { get; set; }

        public List<string> Sent { get; } = [];

        public void Send(string path)
        {
            if (Fails)
            {
                throw new IOException("The shell said no.");
            }

            Sent.Add(path);
        }
    }
}
