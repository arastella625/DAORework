using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Launching;
using DAORework.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Infrastructure.Tests.Launching;

public sealed class ProcessGameRuntimeTests
{
    private static readonly string _exe = MockUnixSupport.Path(@"C:\Games\DAO\bin_ship\daorigins.exe");
    private static readonly string _launcher = MockUnixSupport.Path(@"C:\Games\DAO\DAOriginsLauncher.exe");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeProcessLauncher _launcherPort = new();

    private ProcessGameRuntime CreateRuntime() => new(_fileSystem, _launcherPort, NullLogger<ProcessGameRuntime>.Instance);

    [Fact]
    public async Task LaunchAsync_GameTargetWithoutPath_FailsMissing()
    {
        var result = await CreateRuntime().LaunchAsync(new RuntimeProfile());

        Assert.Equal(DiagnosticCodes.GameExecutableMissing, result.Error!.Code);
        Assert.Null(_launcherPort.LastRequest);
    }

    [Fact]
    public async Task LaunchAsync_LauncherTargetWithAbsentFile_FailsNotFound()
    {
        var profile = new RuntimeProfile { LaunchTarget = LaunchTarget.OfficialLauncher, LauncherExecutablePath = _launcher };

        var result = await CreateRuntime().LaunchAsync(profile);

        Assert.Equal(DiagnosticCodes.LauncherExecutableNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task LaunchAsync_ExecutableExists_StartsItWithArgumentsAndWorkingDirectory()
    {
        _fileSystem.AddFile(_exe, new MockFileData("exe"));
        var profile = new RuntimeProfile { GameExecutablePath = _exe, LaunchArguments = ["-enabledeveloperconsole", "-arg with space"] };

        var result = await CreateRuntime().LaunchAsync(profile);

        Assert.True(result.IsSuccess);
        Assert.Equal(1234, result.Value.ProcessId);
        Assert.Equal(_exe, result.Value.ExecutablePath);
        Assert.NotNull(_launcherPort.LastRequest);
        Assert.Equal(Path.GetDirectoryName(_exe), _launcherPort.LastRequest.WorkingDirectory);
        Assert.Equal(["-enabledeveloperconsole", "-arg with space"], _launcherPort.LastRequest.Arguments);
    }

    [Fact]
    public async Task LaunchAsync_ProcessFailsToStart_PassesTheFailureThrough()
    {
        _fileSystem.AddFile(_exe, new MockFileData("exe"));
        var runtime = new ProcessGameRuntime(_fileSystem, new FakeProcessLauncher { Fail = true }, NullLogger<ProcessGameRuntime>.Instance);

        var result = await runtime.LaunchAsync(new RuntimeProfile { GameExecutablePath = _exe });

        Assert.Equal(DiagnosticCodes.ProcessStartFailed, result.Error!.Code);
    }
}
