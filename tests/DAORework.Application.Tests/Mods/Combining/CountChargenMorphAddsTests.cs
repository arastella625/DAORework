using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Combining;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Xunit;

namespace DAORework.Application.Tests.Mods.Combining;

/// <summary>Counting what library mods add to character creation (P2.10-03). The samples are made up here, not the game's file.</summary>
public sealed class CountChargenMorphAddsTests
{
    private const string Game = """<morph_config><hairs><human_male><resource name="a" cut="0" /></human_male></hairs></morph_config>""";
    private const string Install = "D:/Games/Dragon Age";

    private readonly FakeLibrary _library = new();
    private readonly FakeGame _game = new();

    [Fact]
    public async Task Execute_CountsEachModAgainstTheGamesCopy_AndReadsEachFileOnce()
    {
        _library.Texts["long"] = Game.Replace("</human_male>", """<resource name="b" cut="0" /><resource name="c" cut="0" /></human_male>""", StringComparison.Ordinal);
        _game.Text = Game;
        var count = new CountChargenMorphAdds(_library, _game);

        var tally = await count.ExecuteAsync([Mod("long", "chargenmorphcfg.xml"), Mod("plain", "hair.dds")], Install, CancellationToken.None);
        await count.ExecuteAsync([Mod("long", "chargenmorphcfg.xml")], Install, CancellationToken.None);

        Assert.True(tally.AgainstTheGame);
        Assert.Equal("Adds 2 hairstyles", tally.AddsOf("long")!.Adds());
        Assert.Null(tally.AddsOf("plain"));
        Assert.Equal(1, _library.Reads);
        Assert.Equal(1, _game.Reads);
    }

    [Fact]
    public async Task Execute_WhenTheGamesCopyCantBeRead_CountsAgainstWhatTheModsShare()
    {
        _library.Texts["one"] = Game.Replace("</human_male>", """<resource name="b" cut="0" /></human_male>""", StringComparison.Ordinal);
        _library.Texts["two"] = Game;
        _game.Fails = true;

        var tally = await new CountChargenMorphAdds(_library, _game).ExecuteAsync([Mod("one", "chargenmorphcfg.xml"), Mod("two", "chargenmorphcfg.xml")], Install, CancellationToken.None);

        Assert.False(tally.AgainstTheGame);
        Assert.Equal("Adds 1 hairstyle", tally.AddsOf("one")!.Adds());
    }

    [Fact]
    public async Task Execute_WithNoInstallFolder_OrNoModCopies_NeverReadsTheGame()
    {
        _library.Texts["long"] = Game;

        await new CountChargenMorphAdds(_library, _game).ExecuteAsync([Mod("long", "chargenmorphcfg.xml")], installDirectory: null, CancellationToken.None);
        await new CountChargenMorphAdds(_library, _game).ExecuteAsync([Mod("plain", "hair.dds")], Install, CancellationToken.None);

        Assert.Equal(0, _game.Reads);
    }

    private static LibraryMod Mod(string id, string file) => new(id, id, ModKind.Override, [new LibraryModFile(file, 10, "sha")]);

    private sealed class FakeLibrary : IModLibrary
    {
        public Dictionary<string, string> Texts { get; } = new(StringComparer.Ordinal);

        public int Reads { get; private set; }

        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<string>> ReadTextAsync(string modId, string relativePath, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(Result.Success(Texts[modId]));
        }

        public Task<Result<LibraryModFile>> StoreCombinedAsync(string fileName, string text, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeGame : IGameChargenMorphSource
    {
        public string? Text { get; set; }

        public bool Fails { get; set; }

        public int Reads { get; private set; }

        public Task<Result<string?>> ReadAsync(string installDirectory, CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult(Fails
                ? Result.Failure<string?>(new Error(DiagnosticCodes.GameChargenMorphUnreadable, "In use."))
                : Result.Success(Text));
        }
    }
}
