using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Troubleshooting;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

public sealed class JsonPlayHistoryStoreTests
{
    private const string Documents = "/docs/BioWare/Dragon Age";

    private readonly MockFileSystem _fileSystem = new();
    private readonly DaoReworkOptions _options = new() { AppDataDirectory = "/appdata" };
    private readonly JsonPlayHistoryStore _store;

    public JsonPlayHistoryStoreTests()
    {
        _store = new JsonPlayHistoryStore(_fileSystem, Options.Create(_options));
    }

    [Fact]
    public async Task Load_NothingSaved_IsEmpty() => Assert.Empty((await _store.LoadAsync(Documents)).Value.Plays);

    [Fact]
    public async Task Save_ThenLoad_GivesTheGamesBack()
    {
        var played = new DateTimeOffset(2026, 11, 3, 19, 5, 0, TimeSpan.Zero);

        await _store.SaveAsync(Documents, PlayHistory.Empty.With(new PlayRecord(played, ["a", "b"])));

        var play = Assert.Single((await _store.LoadAsync(Documents)).Value.Plays);
        Assert.Equal(played, play.PlayedAt);
        Assert.Equal(["a", "b"], play.ModIds);
    }

    [Fact]
    public async Task Load_ABrokenFile_SaysSo()
    {
        await _store.SaveAsync(Documents, PlayHistory.Empty);
        await _fileSystem.File.WriteAllTextAsync(_fileSystem.Directory.GetFiles(_options.PlayHistoryDirectory).Single(), "not json");

        Assert.Equal(DiagnosticCodes.PlayHistoryUnreadable, (await _store.LoadAsync(Documents)).Error!.Code);
    }
}
