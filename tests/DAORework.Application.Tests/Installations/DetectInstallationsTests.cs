using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Installations;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Installations;
using Xunit;

namespace DAORework.Application.Tests.Installations;

public sealed class DetectInstallationsTests
{
    private readonly MockFileSystem _fileSystem = new();

    private static Installation Install(string dir, Distribution distribution, bool game = true, bool launcher = false) => new()
    {
        InstallDirectory = dir,
        Distribution = distribution,
        GameExecutablePath = game ? Path.Combine(dir, "bin_ship", "daorigins.exe") : null,
        LauncherExecutablePath = launcher ? Path.Combine(dir, "DAOriginsLauncher.exe") : null,
    };

    [Fact]
    public async Task ExecuteAsync_NoLocators_ReturnsEmpty()
    {
        var result = await new DetectInstallations([], new InstallationProbe(_fileSystem)).ExecuteAsync(new DetectInstallationsRequest());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_MergesLocatorsDedupesAndPutsGameExecutablesFirst()
    {
        var steam = TestPaths.Root("Steam", "DAO");
        var gog = TestPaths.Root("GOG", "DAO");
        var first = new FakeInstallLocator(Install(gog, Distribution.Gog, game: false, launcher: true), Install(steam, Distribution.Steam));
        var second = new FakeInstallLocator(Install(steam.ToUpperInvariant(), Distribution.Steam), Install(TestPaths.Root("Empty"), Distribution.Unknown, game: false));

        var installations = (await new DetectInstallations([first, second], new InstallationProbe(_fileSystem)).ExecuteAsync(new DetectInstallationsRequest())).Value;

        Assert.Equal([steam, gog], installations.Select(i => i.InstallDirectory));
        Assert.True(installations[0].HasValidGameExecutable);
        Assert.False(installations[1].HasValidGameExecutable);
    }

    [Fact]
    public async Task ExecuteAsync_ManualPathWithTheGame_IsReportedAsManual()
    {
        var manual = TestPaths.Root("Games", "DAO on D");
        _fileSystem.AddFile(GamePaths.GameExecutable(manual), new MockFileData("exe"));
        var useCase = new DetectInstallations([], new InstallationProbe(_fileSystem));

        var installations = (await useCase.ExecuteAsync(new DetectInstallationsRequest([manual, TestPaths.Root("Nowhere"), " "]))).Value;

        var installation = Assert.Single(installations);
        Assert.Equal(manual, installation.InstallDirectory);
        Assert.Equal(Distribution.Manual, installation.Distribution);
    }

    [Fact]
    public async Task ExecuteAsync_ManualPathAlsoFoundByALocator_IsListedOnce()
    {
        var steam = TestPaths.Root("Steam", "steamapps", "common", "DAO");
        _fileSystem.AddFile(GamePaths.GameExecutable(steam), new MockFileData("exe"));
        var useCase = new DetectInstallations([new FakeInstallLocator(Install(steam, Distribution.Steam))], new InstallationProbe(_fileSystem));

        var installations = (await useCase.ExecuteAsync(new DetectInstallationsRequest([steam + Path.DirectorySeparatorChar]))).Value;

        Assert.Single(installations);
    }
}
