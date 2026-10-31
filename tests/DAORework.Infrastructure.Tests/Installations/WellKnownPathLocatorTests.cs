using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Installations;
using DAORework.Core.Common;
using DAORework.Core.Installations;
using DAORework.Infrastructure.Installations;
using DAORework.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Infrastructure.Tests.Installations;

public sealed class WellKnownPathLocatorTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeKnownFolders _knownFolders = new();

    private WellKnownPathLocator CreateLocator() => new(new InstallationProbe(_fileSystem), _knownFolders, NullLogger<WellKnownPathLocator>.Instance);

    [Fact]
    public async Task LocateAsync_NothingInstalled_ReturnsEmpty()
    {
        Assert.Empty(await CreateLocator().LocateAsync());
    }

    [Fact]
    public async Task LocateAsync_SteamInstallUnderProgramFilesX86_IsFoundWithItsDistribution()
    {
        var install = Path.Combine(_knownFolders.ProgramFilesX86, "Steam", "steamapps", "common", "Dragon Age Ultimate Edition");
        _fileSystem.AddFile(GamePaths.GameExecutable(install), new MockFileData("exe"));

        var found = await CreateLocator().LocateAsync();

        var installation = Assert.Single(found);
        Assert.Equal(install, installation.InstallDirectory);
        Assert.Equal(Distribution.Steam, installation.Distribution);
        Assert.Equal(GamePaths.GameExecutable(install), installation.GameExecutablePath);
        Assert.Null(installation.LauncherExecutablePath);
    }

    [Fact]
    public async Task LocateAsync_FolderWithoutExecutables_IsNotReported()
    {
        _fileSystem.AddDirectory(Path.Combine(_knownFolders.ProgramFiles, "GOG Galaxy", "Games", "Dragon Age Origins"));

        Assert.Empty(await CreateLocator().LocateAsync());
    }
}
