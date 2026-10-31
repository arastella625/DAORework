using System.IO.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Preferences;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

/// <summary><c>preferences.json</c> (ADR-0017) against a real temp folder.</summary>
public sealed class JsonPreferencesStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));

    private string PreferencesFile => Path.Combine(_directory, "preferences.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task Load_WithNoFile_IsTheDefaults()
    {
        var loaded = await CreateStore().LoadAsync();

        Assert.True(loaded.IsSuccess);
        Assert.Null(loaded.Value.Look);
        Assert.Empty(loaded.Value.ProfileLooks);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsTheLookAndTheProfileLooks()
    {
        var store = CreateStore();
        var preferences = new AppPreferences
        {
            Look = "the-fade",
            ProfileLooks = new Dictionary<string, string> { ["Wilds run"] = "blood-and-shadow" },
        };

        await store.SaveAsync(preferences);
        var loaded = await store.LoadAsync();

        Assert.Equal("the-fade", loaded.Value.Look);
        Assert.Equal("blood-and-shadow", loaded.Value.ProfileLooks["Wilds run"]);
        var json = await File.ReadAllTextAsync(PreferencesFile);
        Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"look\": \"the-fade\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Load_AFileWithoutAVersion_ReadsItAsVersionOne()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(PreferencesFile, "{ \"look\": \"korcari\" }");

        var loaded = await CreateStore().LoadAsync();

        Assert.Equal("korcari", loaded.Value.Look);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{ \"schemaVersion\": 99, \"look\": \"korcari\" }")]
    [InlineData("{ \"look\": 42 }")]
    public async Task Load_AFileThatCannotBeRead_IsAnExpectedFailure(string content)
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(PreferencesFile, content);

        var loaded = await CreateStore().LoadAsync();

        Assert.True(loaded.IsFailure);
        Assert.Equal(DiagnosticCodes.PreferencesUnreadable, loaded.Error!.Code);
    }

    private JsonPreferencesStore CreateStore() =>
        new(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));
}
