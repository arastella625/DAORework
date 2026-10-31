using System.IO.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

/// <summary><c>looks\{id}\look.json</c> (ADR-0017) against a real temp folder.</summary>
public sealed class JsonLookStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));

    private string LooksDirectory => Path.Combine(_directory, "looks");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsTheLookAndItsBackground()
    {
        var store = CreateStore();
        var look = new Look
        {
            Id = "look-a1b2c3",
            Name = "Wilds run",
            BaseTheme = "the-fade",
            Accent = "#3BA3D9",
            Background = new LookBackground { FileName = "background.png", Fit = BackgroundFit.Fit, Tint = 55, Dim = 82, Blur = 8, Brightest = "#D8C9A0" },
        };

        await store.SaveAsync(look);
        var loaded = await store.LoadAsync("look-a1b2c3");

        Assert.Equal(look with { Background = null }, loaded.Value with { Background = null });
        Assert.Equal(look.Background, loaded.Value.Background);
        Assert.Contains("\"schemaVersion\": 1", await File.ReadAllTextAsync(Path.Combine(LooksDirectory, "look-a1b2c3", "look.json")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("look-missing")]
    [InlineData("look-../../escape")]
    [InlineData("../preferences")]
    public async Task Load_AnIdThatIsNotThere_OrIsNotAnId_IsNotFound(string id)
    {
        var loaded = await CreateStore().LoadAsync(id);

        Assert.Equal(DiagnosticCodes.LookNotFound, loaded.Error!.Code);
    }

    [Fact]
    public async Task List_ReturnsTheReadableLooks_AndAnErrorForEachBadOne()
    {
        var store = CreateStore();
        await store.SaveAsync(new Look { Id = "look-good", Name = "Good", BaseTheme = "korcari" });
        Write("look-broken", "not json");
        Write("look-moved", "{ \"schemaVersion\": 1, \"id\": \"look-other\", \"name\": \"Moved\", \"baseTheme\": \"korcari\" }");
        Directory.CreateDirectory(Path.Combine(LooksDirectory, "not-a-look"));

        var listing = await store.ListAsync();

        Assert.Equal(["look-good"], listing.Looks.Select(look => look.Id));
        Assert.Equal(2, listing.Unreadable.Count);
        Assert.All(listing.Unreadable, error => Assert.Equal(DiagnosticCodes.LookUnreadable, error.Code));
    }

    [Fact]
    public async Task Delete_RemovesTheFolderWithItsPicture_AndIsQuietWhenItIsGone()
    {
        var store = CreateStore();
        await store.SaveAsync(new Look { Id = "look-gone", Name = "Gone", BaseTheme = "korcari" });
        await File.WriteAllBytesAsync(Path.Combine(LooksDirectory, "look-gone", "background.png"), [1, 2, 3]);

        await store.DeleteAsync("look-gone");
        await store.DeleteAsync("look-gone");

        Assert.False(Directory.Exists(Path.Combine(LooksDirectory, "look-gone")));
    }

    [Fact]
    public async Task Delete_NeverTouchesAPathThatIsNotALook()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "keep"));

        await CreateStore().DeleteAsync("../keep");

        Assert.True(Directory.Exists(Path.Combine(_directory, "keep")));
    }

    private void Write(string id, string json)
    {
        Directory.CreateDirectory(Path.Combine(LooksDirectory, id));
        File.WriteAllText(Path.Combine(LooksDirectory, id, "look.json"), json);
    }

    private JsonLookStore CreateStore() => new(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));
}
