using DAORework.App.ViewModels;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The diagnostics, mods and launch view models when there is nothing to work on.</summary>
public sealed class PageViewModelTests : IDisposable
{
    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task Diagnostics_NoProfileSelected_DoesNothing()
    {
        var page = _host.Resolve<DiagnosticsPageViewModel>();
        page.Load(null);

        await page.RunDiagnosticsCommand.ExecuteAsync(null);

        Assert.False(page.HasReport);
        Assert.Empty(page.Issues);
    }

    [Fact]
    public async Task Diagnostics_ProfileDeletedMeanwhile_ShowsNotFound()
    {
        var page = _host.Resolve<DiagnosticsPageViewModel>();
        page.Load("Gone");

        await page.RunDiagnosticsCommand.ExecuteAsync(null);

        Assert.True(page.IsError);
        Assert.Contains(DiagnosticCodes.ProfileNotFound, page.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mods_ProfileWithoutDocumentsFolder_ReportsMissingFolders()
    {
        var list = _host.Resolve<ProfileListViewModel>();
        list.NewProfileName = "Bare";
        await list.CreateProfileCommand.ExecuteAsync(null);
        var page = _host.Resolve<ModsPageViewModel>();
        page.Load("Bare");

        await page.InspectCommand.ExecuteAsync(null);

        Assert.False(page.IsError);
        Assert.Equal(2, page.Folders.Count);
        Assert.All(page.Folders, folder => Assert.False(folder.Exists));
    }

    [Fact]
    public async Task Launch_UnknownProfile_ShowsTheErrorAndStartsNothing()
    {
        var launch = _host.Resolve<LaunchViewModel>();
        launch.ProfileName = "Nope";

        await launch.LaunchCommand.ExecuteAsync(null);

        Assert.True(launch.IsError);
        Assert.Contains(DiagnosticCodes.ProfileNotFound, launch.StatusMessage, StringComparison.Ordinal);
        Assert.Null(_host.ProcessLauncher.LastRequest);
    }

    [Fact]
    public void Launch_NoProfile_CannotExecute()
    {
        var launch = _host.Resolve<LaunchViewModel>();

        Assert.False(launch.LaunchCommand.CanExecute(null));
        launch.ProfileName = "Default";
        Assert.True(launch.LaunchCommand.CanExecute(null));
    }
}
