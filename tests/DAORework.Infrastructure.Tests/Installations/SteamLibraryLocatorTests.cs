using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Abstractions;
using DAORework.Application.Installations;
using DAORework.Core.Common;
using DAORework.Core.Installations;
using DAORework.Infrastructure.Installations.Steam;
using DAORework.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Infrastructure.Tests.Installations;

public sealed class SteamLibraryLocatorTests
{
    private static readonly string _secondDrive = MockUnixSupport.Path(@"D:\SteamLibrary");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeRegistry _registry = new();
    private readonly FakeKnownFolders _knownFolders = new();

    private string DefaultSteam => Path.Combine(_knownFolders.ProgramFilesX86, "Steam");

    [Fact]
    public async Task LocateAsync_NoSteamAtAll_ReturnsEmpty()
    {
        Assert.Empty(await CreateLocator().LocateAsync());
    }

    [Fact]
    public async Task LocateAsync_GameOnASecondLibraryDrive_IsFoundThroughTheManifest()
    {
        AddLibraryFolders(DefaultSteam, DefaultSteam, _secondDrive);
        var install = Path.Combine(_secondDrive, "steamapps", "common", "Dragon Age Ultimate Edition");
        _fileSystem.AddFile(Path.Combine(_secondDrive, "steamapps", $"appmanifest_{SteamLibraryLocator.AppId}.acf"), new MockFileData("\"AppState\" { \"installdir\" \"Dragon Age Ultimate Edition\" }"));
        _fileSystem.AddFile(GamePaths.GameExecutable(install), new MockFileData("exe"));

        var found = await CreateLocator().LocateAsync();

        var installation = Assert.Single(found);
        Assert.Equal(install, installation.InstallDirectory);
        Assert.Equal(Distribution.Steam, installation.Distribution);
        Assert.Contains(_secondDrive, installation.Notes, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LocateAsync_SteamRootFromTheRegistry_WithForwardSlashes()
    {
        var steam = MockUnixSupport.Path(@"E:\Steam");
        _registry.Set(RegistryRoot.CurrentUser, @"Software\Valve\Steam", "SteamPath", steam.Replace(Path.DirectorySeparatorChar, '/'));
        var install = Path.Combine(steam, "steamapps", "common", "Dragon Age Origins");
        _fileSystem.AddFile(GamePaths.LauncherExecutable(install), new MockFileData("exe"));

        var found = await CreateLocator().LocateAsync();

        Assert.Equal(install, Assert.Single(found).InstallDirectory);
    }

    [Fact]
    public async Task LocateAsync_NoManifest_FallsBackToTheKnownFolderNames()
    {
        var install = Path.Combine(DefaultSteam, "steamapps", "common", "Dragon Age Ultimate Edition");
        _fileSystem.AddFile(GamePaths.GameExecutable(install), new MockFileData("exe"));

        Assert.Equal(install, Assert.Single(await CreateLocator().LocateAsync()).InstallDirectory);
    }

    [Fact]
    public async Task LocateAsync_CorruptLibraryList_StillChecksTheSteamFolder()
    {
        _fileSystem.AddFile(Path.Combine(DefaultSteam, "steamapps", "libraryfolders.vdf"), new MockFileData("\"libraryfolders\" { \"0\" { \"path\" "));
        var install = Path.Combine(DefaultSteam, "steamapps", "common", "Dragon Age Origins");
        _fileSystem.AddFile(GamePaths.GameExecutable(install), new MockFileData("exe"));

        Assert.Single(await CreateLocator().LocateAsync());
    }

    private void AddLibraryFolders(string steamRoot, params string[] libraries)
    {
        var entries = libraries.Select((path, i) => $"\"{i}\" {{ \"path\" \"{path.Replace(@"\", @"\\")}\" }}");
        _fileSystem.AddFile(Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"), new MockFileData($"\"libraryfolders\" {{ {string.Join(' ', entries)} }}"));
    }

    private SteamLibraryLocator CreateLocator() =>
        new(_fileSystem, _registry, _knownFolders, new InstallationProbe(_fileSystem), NullLogger<SteamLibraryLocator>.Instance);
}
