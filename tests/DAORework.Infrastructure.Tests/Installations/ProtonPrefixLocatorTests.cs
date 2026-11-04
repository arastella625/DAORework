using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Documents;
using DAORework.Infrastructure.Installations.Steam;
using DAORework.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Infrastructure.Tests.Installations;

/// <summary>Finding the game's documents folder inside its Proton prefix (ADR-0031).</summary>
public sealed class ProtonPrefixLocatorTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeKnownFolders _folders = new();

    private string Prefix => ProtonPaths.PrefixInLibrary(Path.Combine(_folders.UserProfile, ".local", "share", "Steam"));

    private ProtonPrefixLocator Locator => new(_fileSystem, new SteamLibraries(_fileSystem, new FakeRegistry(), _folders, NullLogger<SteamLibraries>.Instance));

    [Fact]
    public void Find_NoPrefix_FindsNothing_AndCreatesNothing()
    {
        _fileSystem.AddDirectory(Path.Combine(_folders.UserProfile, ".local", "share", "Steam"));
        var before = _fileSystem.AllDirectories.Count();

        Assert.Empty(Locator.FindPrefixes());
        Assert.Empty(Locator.FindDocumentsFolders());
        Assert.Equal(before, _fileSystem.AllDirectories.Count());
    }

    [Theory]
    [InlineData("steamuser", "Documents")]
    [InlineData("steamuser", "My Documents")]
    [InlineData("deck", "Documents")]
    public void FindDocumentsFolders_TakesTheNamesFromWhatIsOnDisk(string user, string documents)
    {
        var folder = Path.Combine(Prefix, "drive_c", "users", user, documents, "BioWare", "Dragon Age");
        _fileSystem.AddDirectory(folder);

        Assert.Equal(_fileSystem.Path.GetFullPath(folder), Assert.Single(Locator.FindDocumentsFolders()));
    }

    [Fact]
    public void FindDocumentsFolders_APrefixTheGameHasNotWrittenTo_HasNone()
    {
        _fileSystem.AddDirectory(Path.Combine(Prefix, "drive_c", "users", "steamuser", "Documents"));

        Assert.Single(Locator.FindPrefixes());
        Assert.Empty(Locator.FindDocumentsFolders());
    }
}
