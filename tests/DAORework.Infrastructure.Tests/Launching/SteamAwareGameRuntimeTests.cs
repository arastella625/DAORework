using System.IO.Abstractions.TestingHelpers;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Launching;
using DAORework.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Infrastructure.Tests.Launching;

/// <summary>ADR-0022: a Steam install is started by asking Steam; every other install as before.</summary>
public sealed class SteamAwareGameRuntimeTests
{
    private static readonly string _steamInstall = MockUnixSupport.Path(@"D:\SteamLibrary\steamapps\common\Dragon Age Ultimate Edition");
    private static readonly string _gogInstall = MockUnixSupport.Path(@"C:\GOG Games\Dragon Age Origins");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeProcessLauncher _processes = new();
    private readonly FakeUriLauncher _links = new();

    public SteamAwareGameRuntimeTests()
    {
        _fileSystem.AddFile(GamePaths.GameExecutable(_steamInstall), new MockFileData("exe"));
        _fileSystem.AddFile(
            MockUnixSupport.Path(@"D:\SteamLibrary\steamapps\appmanifest_47810.acf"),
            new MockFileData("\"AppState\" { \"appid\" \"47810\" \"installdir\" \"Dragon Age Ultimate Edition\" }"));
        _fileSystem.AddFile(GamePaths.GameExecutable(_gogInstall), new MockFileData("exe"));
    }

    [Fact]
    public async Task SteamInstall_OpensTheSteamLink_AndStartsNoProcess()
    {
        var result = await Runtime(_links).LaunchAsync(Profile(_steamInstall));

        Assert.True(result.IsSuccess);
        Assert.Equal("steam://rungameid/47810", _links.LastUri?.AbsoluteUri);
        Assert.Null(_processes.LastRequest);
        Assert.True(result.Value.StartedBySteam);
        Assert.Equal("steam://rungameid/47810", result.Value.ExecutablePath);
    }

    [Fact]
    public async Task SteamInstallWithArguments_PassesThemThroughSteam()
    {
        var profile = Profile(_steamInstall) with { LaunchArguments = ["-enabledeveloperconsole", "-skipintro"] };

        await Runtime(_links).LaunchAsync(profile);

        Assert.Equal("steam://run/47810//-enabledeveloperconsole%20-skipintro/", _links.LastUri?.AbsoluteUri);
    }

    [Fact]
    public async Task SteamLinkThatCannotOpen_FailsWithTheReason()
    {
        var result = await Runtime(new FakeUriLauncher { Fail = true }).LaunchAsync(Profile(_steamInstall));

        Assert.Equal(DiagnosticCodes.ProcessStartFailed, result.Error!.Code);
        Assert.Null(_processes.LastRequest);
    }

    [Fact]
    public async Task OtherInstall_StartsTheExecutableAsBefore()
    {
        var result = await Runtime(_links).LaunchAsync(Profile(_gogInstall));

        Assert.True(result.IsSuccess);
        Assert.Equal(1234, result.Value.ProcessId);
        Assert.False(result.Value.StartedBySteam);
        Assert.Equal(GamePaths.GameExecutable(_gogInstall), _processes.LastRequest?.FileName);
        Assert.Null(_links.LastUri);
    }

    private static RuntimeProfile Profile(string install) =>
        new() { InstallDirectory = install, GameExecutablePath = GamePaths.GameExecutable(install) };

    private SteamAwareGameRuntime Runtime(FakeUriLauncher links) => new(
        new SteamGameInstall(_fileSystem),
        links,
        new ProcessGameRuntime(_fileSystem, _processes, NullLogger<ProcessGameRuntime>.Instance),
        NullLogger<SteamAwareGameRuntime>.Instance);
}
