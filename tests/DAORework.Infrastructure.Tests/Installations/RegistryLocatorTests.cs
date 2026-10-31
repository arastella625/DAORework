using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Abstractions;
using DAORework.Application.Installations;
using DAORework.Core.Common;
using DAORework.Core.Installations;
using DAORework.Infrastructure.Installations;
using DAORework.Infrastructure.Registry;
using DAORework.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Infrastructure.Tests.Installations;

public sealed class RegistryLocatorTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeRegistry _registry = new();

    [Fact]
    public async Task Gog_GameRegisteredUnderGalaxy_IsFound()
    {
        var install = MockUnixSupport.Path(@"D:\GOG Games\Dragon Age Origins");
        _registry
            .Set(RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\GOG.com\Games\1949616612", "gameName", "Dragon Age: Origins - Ultimate Edition")
            .Set(RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\GOG.com\Games\1949616612", "path", install)
            .Set(RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\GOG.com\Games\1207658930", "gameName", "The Witcher")
            .Set(RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\GOG.com\Games\1207658930", "path", MockUnixSupport.Path(@"D:\GOG Games\The Witcher"));
        _fileSystem.AddFile(GamePaths.GameExecutable(install), new MockFileData("exe"));

        var found = await new GogRegistryLocator(_registry, Probe(), NullLogger<GogRegistryLocator>.Instance).LocateAsync();

        var installation = Assert.Single(found);
        Assert.Equal(install, installation.InstallDirectory);
        Assert.Equal(Distribution.Gog, installation.Distribution);
    }

    [Fact]
    public async Task Gog_RegisteredButNotOnDisk_IsNotReported()
    {
        _registry
            .Set(RegistryRoot.LocalMachine, @"SOFTWARE\GOG.com\Games\1", "gameName", "Dragon Age: Origins")
            .Set(RegistryRoot.LocalMachine, @"SOFTWARE\GOG.com\Games\1", "path", MockUnixSupport.Path(@"D:\Gone"));

        Assert.Empty(await new GogRegistryLocator(_registry, Probe(), NullLogger<GogRegistryLocator>.Instance).LocateAsync());
    }

    [Fact]
    public async Task BioWare_KeyWrittenByTheEaApp_IsFoundAsEaApp()
    {
        var install = MockUnixSupport.Path(@"C:\Games\Dragon Age");
        _registry.Set(RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\BioWare\Dragon Age", "Path", install);
        _fileSystem.AddFile(GamePaths.GameExecutable(install), new MockFileData("exe"));

        var found = await new BioWareRegistryLocator(_registry, Probe(), NullLogger<BioWareRegistryLocator>.Instance).LocateAsync();

        var installation = Assert.Single(found);
        Assert.Equal(install, installation.InstallDirectory);
        Assert.Equal(Distribution.EaApp, installation.Distribution);
    }

    [Fact]
    public async Task BioWare_KeyPointingIntoSteam_KeepsTheSteamDistribution()
    {
        var install = MockUnixSupport.Path(@"C:\Steam\steamapps\common\Dragon Age Ultimate Edition");
        _registry.Set(RegistryRoot.LocalMachine, @"SOFTWARE\BioWare\Dragon Age", "Path", install);
        _fileSystem.AddFile(GamePaths.LauncherExecutable(install), new MockFileData("exe"));

        var found = await new BioWareRegistryLocator(_registry, Probe(), NullLogger<BioWareRegistryLocator>.Instance).LocateAsync();

        Assert.Equal(Distribution.Steam, Assert.Single(found).Distribution);
    }

    [Fact]
    public void WindowsRegistry_OffWindows_FindsNothing()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var registry = new WindowsRegistry();

        Assert.Null(registry.GetStringValue(RegistryRoot.LocalMachine, @"SOFTWARE\BioWare\Dragon Age", "Path"));
        Assert.Empty(registry.GetSubKeyNames(RegistryRoot.LocalMachine, @"SOFTWARE\GOG.com\Games"));
    }

    private InstallationProbe Probe() => new(_fileSystem);
}
