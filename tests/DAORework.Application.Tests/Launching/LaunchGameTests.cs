using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics;
using DAORework.Application.Launching;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Application.Tests.Launching;

public sealed class LaunchGameTests
{
    private static readonly string _install = TestPaths.Root("Games", "DAO");
    private static readonly string _exe = GamePaths.GameExecutable(_install);

    private readonly MockFileSystem _fileSystem = new();
    private readonly InMemoryProfileRepository _store = new();
    private readonly FakeReportWriter _reportWriter = new();
    private readonly FakeGameRuntimeResolver _resolver = new();
    private readonly FakeGameRuntime _runtime = new();

    public LaunchGameTests()
    {
        _resolver.Runtimes[RuntimeMode.OriginalGame] = _runtime;
    }

    private LaunchGame CreateUseCase() => new(
        _store,
        new RunDiagnostics(TestDiagnosticRules.All(_fileSystem), new FakeTimeProvider()),
        _reportWriter,
        _resolver,
        NullLogger<LaunchGame>.Instance);

    private RuntimeProfile AddValidProfile(string name = "Default")
    {
        _fileSystem.AddFile(_exe, new MockFileData("exe"));
        var profile = new RuntimeProfile { Name = name, InstallDirectory = _install, GameExecutablePath = _exe };
        _store.Profiles[name] = profile;
        return profile;
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProfile_FailsWithoutCreatingOrSavingAnything()
    {
        var result = await CreateUseCase().ExecuteAsync(new LaunchGameRequest("Missing"));

        Assert.True(result.IsFailure);
        Assert.Equal(DiagnosticCodes.ProfileNotFound, result.Error!.Code);
        Assert.Equal(0, _store.SaveCount);
        Assert.Empty(_store.Profiles);
        Assert.Empty(_reportWriter.Written);
        Assert.Null(_resolver.RequestedMode);
    }

    [Fact]
    public async Task ExecuteAsync_ProfileFailsValidation_WritesReportAndFailsWithItsPath()
    {
        _store.Profiles["Default"] = new RuntimeProfile { Name = "Default" };

        var result = await CreateUseCase().ExecuteAsync(new LaunchGameRequest("Default"));

        Assert.Equal(DiagnosticCodes.ProfileValidationFailed, result.Error!.Code);
        Assert.Contains("report-1.json", result.Error.Recommendation, StringComparison.Ordinal);
        Assert.Single(_reportWriter.Written);
        Assert.Null(_resolver.RequestedMode);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_ValidProfile_LaunchesThroughTheRuntimeForItsMode()
    {
        var profile = AddValidProfile();

        var result = await CreateUseCase().ExecuteAsync(new LaunchGameRequest("Default"));

        Assert.True(result.IsSuccess);
        Assert.Equal(4242, result.Value.ProcessId);
        Assert.Equal(RuntimeMode.OriginalGame, _resolver.RequestedMode);
        Assert.Same(profile, _runtime.LaunchedProfile);
        Assert.Single(_reportWriter.Written);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_ModeWithoutRuntime_FailsUnsupported()
    {
        _store.Profiles["X"] = AddValidProfile("X") with { RuntimeMode = (RuntimeMode)99 };

        var result = await CreateUseCase().ExecuteAsync(new LaunchGameRequest("X"));

        Assert.Equal(DiagnosticCodes.RuntimeModeUnsupported, result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_RuntimeFails_PassesTheFailureThrough()
    {
        AddValidProfile();
        var failing = new FakeGameRuntime { Outcome = Result.Failure<LaunchResult>(new Error(DiagnosticCodes.ProcessStartFailed, "nope")) };
        _resolver.Runtimes[RuntimeMode.OriginalGame] = failing;

        var result = await CreateUseCase().ExecuteAsync(new LaunchGameRequest("Default"));

        Assert.Equal(DiagnosticCodes.ProcessStartFailed, result.Error!.Code);
    }
}
