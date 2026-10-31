using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DAORework.App.Composition;
using DAORework.App.Services;
using DAORework.App.Tests.Fakes;
using DAORework.Application;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DAORework.App.Tests;

/// <summary>
/// The real composition with the machine ports swapped for fakes: an in-memory file system for the game and mod
/// folders, fake known folders, a fake process launcher and a scripted path picker. Profiles and reports still go
/// through the real JSON adapters into a temp directory.
/// </summary>
internal sealed class AppTestHost : IDisposable
{
    private readonly string _appData = Path.Combine(Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));
    private readonly ServiceProvider _services;

    public AppTestHost()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(options => options.AppDataDirectory = _appData)
            .AddAppViewModels();
        services.Replace(ServiceDescriptor.Singleton<IFileSystem>(FileSystem));
        services.Replace(ServiceDescriptor.Singleton<IKnownFolders>(KnownFolders));
        services.Replace(ServiceDescriptor.Singleton<IProcessLauncher>(ProcessLauncher));
        services.Replace(ServiceDescriptor.Singleton<IUriLauncher>(UriLauncher));
        services.Replace(ServiceDescriptor.Singleton<IPathPicker>(Picker));
        services.Replace(ServiceDescriptor.Singleton<IRegistry>(new EmptyRegistry()));
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    /// <summary>Gets a Steam library on a second drive, the case the well-known folders never covered.</summary>
    public static string SteamLibrary { get; } = MockUnixSupport.Path(@"D:\SteamLibrary");

    public static string InstallDirectory { get; } = Path.Combine(SteamLibrary, "steamapps", "common", "Dragon Age Ultimate Edition");

    public MockFileSystem FileSystem { get; } = new();

    public FakeKnownFolders KnownFolders { get; } = new();

    public FakeProcessLauncher ProcessLauncher { get; } = new();

    public FakeUriLauncher UriLauncher { get; } = new();

    public FakePathPicker Picker { get; } = new();

    public string ProfilesDirectory => Path.Combine(_appData, "profiles");

    public string PreferencesFile => Path.Combine(_appData, "preferences.json");

    public T Resolve<T>()
        where T : notnull
        => _services.GetRequiredService<T>();

    /// <summary>A complete install and a documents folder with a few mod files, so screens have something to show.</summary>
    public void SeedRealisticInstall()
    {
        FileSystem.AddFile(GamePaths.GameExecutable(InstallDirectory), new MockFileData([0x4D, 0x5A]));
        FileSystem.AddFile(GamePaths.LauncherExecutable(InstallDirectory), new MockFileData([0x4D, 0x5A]));

        // Steam under Program Files (x86) with a library list that names the second drive.
        var steam = Path.Combine(KnownFolders.ProgramFilesX86, "Steam");
        var libraries = $"\"libraryfolders\" {{ \"0\" {{ \"path\" \"{steam.Replace(@"\", @"\\")}\" }} \"1\" {{ \"path\" \"{SteamLibrary.Replace(@"\", @"\\")}\" }} }}";
        FileSystem.AddFile(Path.Combine(steam, "steamapps", "libraryfolders.vdf"), new MockFileData(libraries));
        FileSystem.AddFile(Path.Combine(SteamLibrary, "steamapps", "appmanifest_47810.acf"), new MockFileData("\"AppState\" { \"appid\" \"47810\" \"installdir\" \"Dragon Age Ultimate Edition\" }"));

        var documents = GamePaths.DefaultDocumentsDirectory(KnownFolders.MyDocuments);
        FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(documents), "qwinn_fixpack", "fixes.gda"), new MockFileData(new byte[2048]));
        FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(documents), "textures", "hair_01.dds"), new MockFileData(new byte[65536]));
        FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(documents), "dupe.gda"), new MockFileData(new byte[10]));
        FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(documents), "other", "dupe.gda"), new MockFileData(new byte[10]));
        FileSystem.AddFile(Path.Combine(GamePaths.AddInsDirectory(documents), "dao_prc_ep_1", "manifest.xml"), new MockFileData(new byte[512]));
        FileSystem.AddFile(Path.Combine(GamePaths.CharactersDirectory(documents), "Aeducan", "Saves", "Slot_1", "Slot_1.das"), new MockFileData(new byte[40960]));
        FileSystem.AddFile(Path.Combine(GamePaths.CharactersDirectory(documents), "Aeducan", "Saves", "Slot_1", "screen.dds"), new MockFileData(new byte[8192]));
    }

    public void Dispose()
    {
        _services.Dispose();
        if (Directory.Exists(_appData))
        {
            Directory.Delete(_appData, recursive: true);
        }
    }
}
