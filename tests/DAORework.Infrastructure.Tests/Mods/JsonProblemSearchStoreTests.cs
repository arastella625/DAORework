using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Troubleshooting;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class JsonProblemSearchStoreTests
{
    private const string Documents = "/docs/BioWare/Dragon Age";

    private readonly MockFileSystem _fileSystem = new();
    private readonly DaoReworkOptions _options = new() { AppDataDirectory = "/appdata" };
    private readonly JsonProblemSearchStore _store;

    public JsonProblemSearchStoreTests()
    {
        _store = new JsonProblemSearchStore(_fileSystem, Options.Create(_options));
    }

    [Fact]
    public async Task Load_NothingSaved_IsNoSearch() => Assert.Null((await _store.LoadAsync(Documents)).Value);

    [Fact]
    public async Task Save_ThenLoad_GivesTheSameRoundBack()
    {
        var search = ProblemSearch.Start(["a", "b", "c", "d"]).Answer(stillHappens: false);

        await _store.SaveAsync(Documents, search);
        var loaded = (await _store.LoadAsync(Documents)).Value!;

        Assert.Equal(search.Original, loaded.Original);
        Assert.Equal(search.Suspects, loaded.Suspects);
        Assert.Equal(search.Off, loaded.Off);
        Assert.Equal(2, loaded.Round);
        Assert.True(loaded.Proven);
    }

    [Fact]
    public async Task Save_AFinishedSearch_KeepsHowItEnded()
    {
        var search = ProblemSearch.Start(["only"]).Answer(stillHappens: false);

        await _store.SaveAsync(Documents, search);
        var loaded = (await _store.LoadAsync(Documents)).Value!;

        Assert.Equal(ProblemSearchEnd.Found, loaded.End);
        Assert.Equal("only", loaded.Culprit);
    }

    [Fact]
    public async Task Delete_ThenLoad_IsNoSearch()
    {
        await _store.SaveAsync(Documents, ProblemSearch.Start(["a", "b"]));

        _store.Delete(Documents);

        Assert.Null((await _store.LoadAsync(Documents)).Value);
    }

    [Fact]
    public void Delete_NothingEverSaved_DoesNothing() => _store.Delete(Documents);

    [Fact]
    public async Task Load_ABrokenFile_SaysSo_AndHowToGetPastIt()
    {
        await _store.SaveAsync(Documents, ProblemSearch.Start(["a", "b"]));
        var path = _fileSystem.Directory.GetFiles(_options.ProblemSearchesDirectory).Single();
        await _fileSystem.File.WriteAllTextAsync(path, "not json");

        var result = await _store.LoadAsync(Documents);

        Assert.Equal(DiagnosticCodes.ModSearchUnreadable, result.Error!.Code);
    }
}
