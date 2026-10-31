using DAORework.Application.Profiles;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Profiles;

public sealed class UpdateProfilePathTests
{
    private readonly InMemoryProfileRepository _repository = new();

    public UpdateProfilePathTests()
    {
        _repository.Profiles["A"] = new RuntimeProfile { Name = "A" };
    }

    [Theory]
    [InlineData(ProfilePathKind.Install)]
    [InlineData(ProfilePathKind.Game)]
    [InlineData(ProfilePathKind.Launcher)]
    [InlineData(ProfilePathKind.Documents)]
    public async Task ExecuteAsync_EachKind_SetsOnlyThatPathAndSaves(ProfilePathKind kind)
    {
        var path = TestPaths.Root("Somewhere");

        var profile = (await new UpdateProfilePath(_repository).ExecuteAsync(new UpdateProfilePathRequest("A", kind, $" {path} "))).Value;

        var values = new[] { profile.InstallDirectory, profile.GameExecutablePath, profile.LauncherExecutablePath, profile.UserDocumentsDirectory };
        Assert.Equal(path, values[(int)kind]);
        Assert.Equal(1, values.Count(v => v is not null));
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyPath_FailsWithoutSaving()
    {
        var result = await new UpdateProfilePath(_repository).ExecuteAsync(new UpdateProfilePathRequest("A", ProfilePathKind.Game, "  "));

        Assert.Equal(DiagnosticCodes.ProfilePathInvalid, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_Unknown_FailsNotFound()
    {
        var result = await new UpdateProfilePath(_repository).ExecuteAsync(new UpdateProfilePathRequest("Nope", ProfilePathKind.Game, "x"));

        Assert.Equal(DiagnosticCodes.ProfileNotFound, result.Error!.Code);
    }
}
