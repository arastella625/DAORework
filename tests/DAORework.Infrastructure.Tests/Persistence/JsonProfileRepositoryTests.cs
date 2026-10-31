using System.IO.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Persistence;

public sealed class JsonProfileRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsEveryProperty()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));
        var profile = new RuntimeProfile
        {
            Name = "RoundTrip",
            InstallDirectory = @"C:\Games\DAO",
            GameExecutablePath = @"C:\Games\DAO\bin_ship\daorigins.exe",
            LauncherExecutablePath = @"C:\Games\DAO\DAOriginsLauncher.exe",
            UserDocumentsDirectory = @"C:\Users\Me\Documents\BioWare\Dragon Age",
            LaunchTarget = LaunchTarget.OfficialLauncher,
            RuntimeMode = RuntimeMode.OriginalGame,
            ActiveModProfileName = "Mods",
            LaunchArguments = ["-enabledeveloperconsole"],
            Metadata = new Dictionary<string, string> { ["source"] = "test" },
        };

        await store.SaveAsync(profile);
        var loaded = await store.LoadAsync("RoundTrip");

        Assert.NotNull(loaded);
        Assert.Equal(profile.Name, loaded.Name);
        Assert.Equal(profile.InstallDirectory, loaded.InstallDirectory);
        Assert.Equal(profile.GameExecutablePath, loaded.GameExecutablePath);
        Assert.Equal(profile.LauncherExecutablePath, loaded.LauncherExecutablePath);
        Assert.Equal(profile.UserDocumentsDirectory, loaded.UserDocumentsDirectory);
        Assert.Equal(profile.LaunchTarget, loaded.LaunchTarget);
        Assert.Equal(profile.RuntimeMode, loaded.RuntimeMode);
        Assert.Equal(profile.ActiveModProfileName, loaded.ActiveModProfileName);
        Assert.Equal(profile.LaunchArguments, loaded.LaunchArguments);
        Assert.Equal("test", loaded.Metadata["source"]);
    }

    [Fact]
    public async Task LoadAsync_UnknownProfile_ReturnsNull()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));

        var loaded = await store.LoadAsync("DoesNotExist");

        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveThenList_NameWithSpaces_IsStoredAndListedVerbatim()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));

        await store.SaveAsync(new RuntimeProfile { Name = "My Profile" });

        Assert.Equal(["My Profile"], await store.ListNamesAsync());
        Assert.NotNull(await store.LoadAsync("My Profile"));
    }

    [Fact]
    public async Task LoadAsync_InvalidName_ReturnsNull()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));

        Assert.Null(await store.LoadAsync("My:Profile"));
    }

    [Fact]
    public async Task SaveAsync_InvalidName_ThrowsArgumentException()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(new RuntimeProfile { Name = "bad*name" }));
    }

    [Fact]
    public async Task SaveAsync_StampsTheSchemaVersionAndLeavesNoTempFile()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));

        await store.SaveAsync(new RuntimeProfile { Name = "Stamped", SchemaVersion = 0 });

        var path = Path.Combine(_directory, "profiles", "Stamped.json");
        Assert.Contains($"\"schemaVersion\": {RuntimeProfile.CurrentSchemaVersion}", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        Assert.Equal(["Stamped.json"], Directory.GetFiles(Path.Combine(_directory, "profiles")).Select(Path.GetFileName));
    }

    [Fact]
    public async Task LoadAsync_LegacyDocumentWithoutVersion_IsMigratedToTheFileName()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));
        const string legacy = """{"name":"My:Profile","launchTarget":1,"installDirectory":"C:\\DAO"}""";
        await File.WriteAllTextAsync(Path.Combine(_directory, "profiles", "My_Profile.json"), legacy);

        var loaded = await store.LoadAsync("My_Profile");

        Assert.NotNull(loaded);
        Assert.Equal("My_Profile", loaded.Name);
        Assert.Equal(RuntimeProfile.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(LaunchTarget.OfficialLauncher, loaded.LaunchTarget);
        Assert.Equal(@"C:\DAO", loaded.InstallDirectory);
        Assert.Contains("My:Profile", await File.ReadAllTextAsync(Path.Combine(_directory, "profiles", "My_Profile.json")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_NewerDocument_ThrowsInvalidData()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));
        await File.WriteAllTextAsync(Path.Combine(_directory, "profiles", "Future.json"), """{"schemaVersion":99,"name":"Future"}""");

        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync("Future"));
    }

    [Fact]
    public async Task LoadAsync_CorruptDocument_ThrowsInvalidDataNamingTheFile()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));
        await File.WriteAllTextAsync(Path.Combine(_directory, "profiles", "Empty.json"), string.Empty);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync("Empty"));

        Assert.Contains("Empty.json", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListNamesAsync_AfterTwoSaves_ListsBothNames()
    {
        var store = new JsonProfileRepository(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));
        await store.SaveAsync(new RuntimeProfile { Name = "Alpha" });
        await store.SaveAsync(new RuntimeProfile { Name = "Beta" });

        var names = await store.ListNamesAsync();

        Assert.Equal(["Alpha", "Beta"], names.Order());
    }
}
