using DAORework.Application.Profiles;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Profiles;

public sealed class UpdateLaunchTargetTests
{
    private readonly InMemoryProfileRepository _repository = new();

    [Fact]
    public async Task ExecuteAsync_Known_UpdatesAndSaves()
    {
        _repository.Profiles["A"] = new RuntimeProfile { Name = "A", InstallDirectory = "x" };

        var result = await new UpdateLaunchTarget(_repository).ExecuteAsync(new UpdateLaunchTargetRequest("A", LaunchTarget.OfficialLauncher));

        Assert.Equal(LaunchTarget.OfficialLauncher, result.Value.LaunchTarget);
        Assert.Equal("x", result.Value.InstallDirectory);
        Assert.Equal(LaunchTarget.OfficialLauncher, _repository.Profiles["A"].LaunchTarget);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_Unknown_FailsNotFound()
    {
        var result = await new UpdateLaunchTarget(_repository).ExecuteAsync(new UpdateLaunchTargetRequest("Nope", LaunchTarget.Game));

        Assert.Equal(DiagnosticCodes.ProfileNotFound, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }
}
