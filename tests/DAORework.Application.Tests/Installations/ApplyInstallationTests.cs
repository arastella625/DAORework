using DAORework.Application.Installations;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Installations;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Installations;

public sealed class ApplyInstallationTests
{
    private readonly InMemoryProfileRepository _repository = new();

    public ApplyInstallationTests()
    {
        _repository.Profiles["A"] = new RuntimeProfile { Name = "A", LauncherExecutablePath = TestPaths.Root("Old", "DAOriginsLauncher.exe") };
    }

    [Fact]
    public async Task ExecuteAsync_SetsTheDirectoryAndFoundExecutables_KeepingUnknownOnes()
    {
        var root = TestPaths.Root("Steam", "DAO");
        var installation = new Installation { InstallDirectory = root, GameExecutablePath = TestPaths.Root("Steam", "DAO", "bin_ship", "daorigins.exe"), Distribution = Distribution.Steam };

        var profile = (await new ApplyInstallation(_repository).ExecuteAsync(new ApplyInstallationRequest("A", installation))).Value;

        Assert.Equal(root, profile.InstallDirectory);
        Assert.Equal(installation.GameExecutablePath, profile.GameExecutablePath);
        Assert.Equal(TestPaths.Root("Old", "DAOriginsLauncher.exe"), profile.LauncherExecutablePath);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProfile_FailsWithoutSaving()
    {
        var installation = new Installation { InstallDirectory = TestPaths.Root("X"), Distribution = Distribution.Manual };

        var result = await new ApplyInstallation(_repository).ExecuteAsync(new ApplyInstallationRequest("Nope", installation));

        Assert.Equal(DiagnosticCodes.ProfileNotFound, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }
}
