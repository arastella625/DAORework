using DAORework.App.ViewModels;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The profile selector and page over the real use cases, without a window.</summary>
public sealed class ProfileViewModelTests : IDisposable
{
    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task CreateProfile_InvalidName_ShowsTheErrorAndCreatesNothing()
    {
        var list = _host.Resolve<ProfileListViewModel>();
        list.NewProfileName = "bad:name";

        await list.CreateProfileCommand.ExecuteAsync(null);

        Assert.True(list.IsError);
        Assert.Contains(DiagnosticCodes.ProfileNameInvalidCharacters, list.StatusMessage, StringComparison.Ordinal);
        Assert.Empty(list.Profiles);
    }

    [Fact]
    public async Task CreateProfile_ValidName_ListsAndSelectsIt()
    {
        var list = _host.Resolve<ProfileListViewModel>();
        string? announced = null;
        list.SelectionChanged += (_, name) => announced = name;
        list.NewProfileName = "Steam";

        await list.CreateProfileCommand.ExecuteAsync(null);

        Assert.False(list.IsError);
        Assert.Equal(["Steam"], list.Profiles);
        Assert.Equal("Steam", list.SelectedProfile);
        Assert.Equal("Steam", announced);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(_host.ProfilesDirectory, "Steam.json")));
    }

    [Fact]
    public async Task Browse_Cancelled_ChangesNothing()
    {
        var page = await CreateProfilePageAsync("Default");

        await page.BrowseCommand.ExecuteAsync(ProfilePathKind.Install);

        Assert.Null(page.InstallDirectory);
        Assert.False(page.IsError);
        Assert.Equal(["Choose the Dragon Age: Origins install folder"], _host.Picker.Titles);
    }

    [Fact]
    public async Task Browse_Chosen_SavesThePathThroughTheUseCase()
    {
        var page = await CreateProfilePageAsync("Default");
        _host.Picker.Enqueue(AppTestHost.InstallDirectory);

        await page.BrowseCommand.ExecuteAsync(ProfilePathKind.Install);

        Assert.Equal(AppTestHost.InstallDirectory, page.InstallDirectory);
        Assert.Contains("install directory", page.StatusMessage, StringComparison.Ordinal);
        var reloaded = _host.Resolve<ProfilePathsViewModel>();
        await reloaded.LoadAsync("Default", TestContext.Current.CancellationToken);
        Assert.Equal(AppTestHost.InstallDirectory, reloaded.InstallDirectory);
    }

    [Fact]
    public async Task SetLaunchTarget_FlipsTheRadioButtonsAndPersists()
    {
        var page = await CreateProfilePageAsync("Default");
        Assert.True(page.IsGameTarget);

        await page.SetLaunchTargetCommand.ExecuteAsync(LaunchTarget.OfficialLauncher);

        Assert.True(page.IsLauncherTarget);
        Assert.False(page.IsGameTarget);
        var reloaded = _host.Resolve<ProfilePathsViewModel>();
        await reloaded.LoadAsync("Default", TestContext.Current.CancellationToken);
        Assert.True(reloaded.IsLauncherTarget);
    }

    [Fact]
    public async Task Load_UnknownProfile_ShowsNotFound()
    {
        var page = _host.Resolve<ProfilePathsViewModel>();

        await page.LoadAsync("Nope", TestContext.Current.CancellationToken);

        Assert.False(page.HasProfile);
        Assert.True(page.IsError);
        Assert.Contains(DiagnosticCodes.ProfileNotFound, page.StatusMessage, StringComparison.Ordinal);
    }

    private async Task<ProfilePathsViewModel> CreateProfilePageAsync(string name)
    {
        var list = _host.Resolve<ProfileListViewModel>();
        list.NewProfileName = name;
        await list.CreateProfileCommand.ExecuteAsync(null);

        var page = _host.Resolve<ProfilePathsViewModel>();
        await page.LoadAsync(name, TestContext.Current.CancellationToken);
        Assert.True(page.HasProfile);
        return page;
    }
}
