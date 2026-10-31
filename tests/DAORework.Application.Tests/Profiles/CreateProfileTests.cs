using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Documents;
using DAORework.Application.Installations;
using DAORework.Application.Profiles;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Installations;
using Xunit;

namespace DAORework.Application.Tests.Profiles;

public sealed class CreateProfileTests
{
    private readonly InMemoryProfileRepository _repository = new();
    private readonly FakeInstallLocator _locator = new();
    private readonly FakeKnownFolders _knownFolders = new();

    private readonly MockFileSystem _fileSystem = new();

    private CreateProfile CreateUseCase() => new(
        _repository,
        new DetectInstallations([_locator], new InstallationProbe(_fileSystem)),
        new DetectDocumentsFolders(_fileSystem, _knownFolders));

    [Fact]
    public async Task ExecuteAsync_InvalidName_FailsAndSavesNothing()
    {
        var result = await CreateUseCase().ExecuteAsync(new CreateProfileRequest("bad:name"));

        Assert.Equal(DiagnosticCodes.ProfileNameInvalidCharacters, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_NoInstallDetected_SavesProfileWithDefaultDocumentsOnly()
    {
        var result = await CreateUseCase().ExecuteAsync(new CreateProfileRequest(" Default "));

        Assert.True(result.IsSuccess);
        var profile = result.Value;
        Assert.Equal("Default", profile.Name);
        Assert.Null(profile.InstallDirectory);
        Assert.Null(profile.GameExecutablePath);
        Assert.Equal(GamePaths.DefaultDocumentsDirectory(_knownFolders.MyDocuments), profile.UserDocumentsDirectory);
        Assert.Same(profile, _repository.Profiles["Default"]);
    }

    [Fact]
    public async Task ExecuteAsync_SavesExistOnlyUnderTheClassicDocuments_PrefersThatFolder()
    {
        var classic = GamePaths.DefaultDocumentsDirectory(Path.Combine(_knownFolders.UserProfile, "Documents"));
        _fileSystem.AddDirectory(classic);
        var redirected = new FakeKnownFolders { MyDocuments = TestPaths.Root("Users", "Me", "OneDrive", "Documents"), OneDrive = TestPaths.Root("Users", "Me", "OneDrive") };
        var useCase = new CreateProfile(_repository, new DetectInstallations([], new InstallationProbe(_fileSystem)), new DetectDocumentsFolders(_fileSystem, redirected));

        var profile = (await useCase.ExecuteAsync(new CreateProfileRequest("Default"))).Value;

        Assert.Equal(classic, profile.UserDocumentsDirectory);
    }

    [Fact]
    public async Task ExecuteAsync_InstallDetected_PrefillsPathsFromTheBestInstall()
    {
        var install = TestPaths.Root("Games", "DAO");
        _locator.Installations.Add(new Installation
        {
            InstallDirectory = install,
            GameExecutablePath = GamePaths.GameExecutable(install),
            LauncherExecutablePath = GamePaths.LauncherExecutable(install),
            Distribution = Distribution.Steam,
        });

        var profile = (await CreateUseCase().ExecuteAsync(new CreateProfileRequest("Steam"))).Value;

        Assert.Equal(install, profile.InstallDirectory);
        Assert.Equal(GamePaths.GameExecutable(install), profile.GameExecutablePath);
        Assert.Equal(GamePaths.LauncherExecutable(install), profile.LauncherExecutablePath);
    }
}
