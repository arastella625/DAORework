using System.IO.Abstractions.TestingHelpers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The Fixes tab (P2.5-06): the 4 GB patch and DXVK applied and removed through a plan, and a fix Steam undid.</summary>
public sealed class FixesScreenTests : IDisposable
{
    private const int FixesTab = 4;
    private static readonly string _release = MockUnixSupport.Path(@"C:\Users\Me\Downloads\dxvk-2.3.zip");

    private readonly AppTestHost _host = new();
    private readonly byte[] _original = FixFixtures.Executable();

    private static string Exe => GamePaths.GameExecutable(AppTestHost.InstallDirectory);

    private static string D3D9 => Path.Combine(AppTestHost.InstallDirectory, "bin_ship", "d3d9.dll");

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task LargeAddressAware_ShowsThePlan_ThenApplies_AndShowsWhenSteamUndidIt()
    {
        var (window, viewModel) = await OpenFixesTabAsync();
        var change = viewModel.Fixes.Change;

        await change.ApplyLargeAddressAwareCommand.ExecuteAsync(null);

        Assert.False(change.IsError, change.StatusMessage);
        Assert.Equal("Apply 4 GB patch", change.PlanTitle);
        Assert.Contains("  Replace bin_ship/daorigins.exe", change.PlanLines);
        Assert.True(change.ApplyCommand.CanExecute(null));
        Assert.Equal(_original, _host.FileSystem.File.ReadAllBytes(Exe));
        Screenshots.Save(window, "18-fixes-plan");

        await change.ApplyCommand.ExecuteAsync(null);

        Assert.False(change.IsError, change.StatusMessage);
        Assert.StartsWith("Done: ", change.StatusMessage, StringComparison.Ordinal);
        Assert.True(FixFixtures.LargeAddressAware(_host.FileSystem.File.ReadAllBytes(Exe)));
        var applied = Assert.Single(viewModel.Fixes.Status.Applied);
        Assert.True(applied.InEffect);
        Assert.StartsWith("4 GB patch", applied.Title, StringComparison.Ordinal);

        // What Steam's Verify integrity does: it puts the original executable back.
        _host.FileSystem.File.WriteAllBytes(Exe, _original);
        await viewModel.Fixes.Status.RefreshAsync(TestContext.Current.CancellationToken);

        var undone = Assert.Single(viewModel.Fixes.Status.Applied);
        Assert.False(undone.InEffect);
        Assert.Contains("Steam's Verify integrity", undone.State, StringComparison.Ordinal);
        Screenshots.Save(window, "19-fixes-undone");
    }

    [AvaloniaFact]
    public async Task Dxvk_InstalledFromAPickedRelease_ThenRemoved()
    {
        _host.FileSystem.AddFile(_release, new MockFileData(FixFixtures.DxvkRelease("2.3")));
        var (_, viewModel) = await OpenFixesTabAsync();
        var change = viewModel.Fixes.Change;
        _host.Picker.Enqueue(_release);

        await change.InstallDxvkCommand.ExecuteAsync(null);

        Assert.False(change.IsError, change.StatusMessage);
        Assert.Equal("Apply DXVK", change.PlanTitle);
        Assert.Equal("Choose the DXVK release you downloaded", Assert.Single(_host.Picker.Titles));
        Assert.False(_host.FileSystem.File.Exists(D3D9));

        await change.ApplyCommand.ExecuteAsync(null);

        Assert.False(change.IsError, change.StatusMessage);
        Assert.True(_host.FileSystem.File.Exists(D3D9));
        Assert.Equal("DXVK 2.3", Assert.Single(viewModel.Fixes.Status.Applied).Title);

        await change.RemoveDxvkCommand.ExecuteAsync(null);
        Assert.Equal("Remove DXVK", change.PlanTitle);
        await change.ApplyCommand.ExecuteAsync(null);

        Assert.False(change.IsError, change.StatusMessage);
        Assert.False(_host.FileSystem.File.Exists(D3D9));
        Assert.Empty(viewModel.Fixes.Status.Applied);
    }

    [AvaloniaFact]
    public async Task RollBack_AFixChange_AskedOnceMore_ThenPutsTheOriginalBack()
    {
        var (window, viewModel) = await OpenFixesTabAsync();
        await viewModel.Fixes.Change.ApplyLargeAddressAwareCommand.ExecuteAsync(null);
        await viewModel.Fixes.Change.ApplyCommand.ExecuteAsync(null);
        var status = viewModel.Fixes.Status;
        var backup = Assert.Single(status.Backups);
        Assert.Equal("Before it: no fixes", backup.Before);

        await status.RollBackToCommand.ExecuteAsync(backup);

        Assert.StartsWith($"Undo the change backed up as {backup.Id}", status.PendingRollback, StringComparison.Ordinal);
        Assert.EndsWith("the fixes applied before it are the applied ones again.", status.PendingRollback, StringComparison.Ordinal);
        Assert.True(FixFixtures.LargeAddressAware(_host.FileSystem.File.ReadAllBytes(Exe)));
        Screenshots.Save(window, "20-fixes-rollback");

        await status.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.False(status.IsError, status.StatusMessage);
        Assert.StartsWith("Rolled back: 1 file(s) put back", status.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(_original, _host.FileSystem.File.ReadAllBytes(Exe));
        Assert.Empty(status.Applied);
    }

    [AvaloniaFact]
    public async Task RollBack_Cancelled_KeepsTheFix()
    {
        var (_, viewModel) = await OpenFixesTabAsync();
        await viewModel.Fixes.Change.ApplyLargeAddressAwareCommand.ExecuteAsync(null);
        await viewModel.Fixes.Change.ApplyCommand.ExecuteAsync(null);
        var status = viewModel.Fixes.Status;
        await status.RollBackToCommand.ExecuteAsync(status.Backups[0]);

        status.CancelRollbackCommand.Execute(null);
        await status.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.Equal("Nothing was changed.", status.StatusMessage);
        Assert.True(FixFixtures.LargeAddressAware(_host.FileSystem.File.ReadAllBytes(Exe)));
        Assert.Single(status.Applied);
    }

    [AvaloniaFact]
    public async Task Dxvk_PickerCancelled_ChangesNothing()
    {
        var (_, viewModel) = await OpenFixesTabAsync();

        await viewModel.Fixes.Change.InstallDxvkCommand.ExecuteAsync(null);

        Assert.Null(viewModel.Fixes.Change.PlanTitle);
        Assert.Equal(string.Empty, viewModel.Fixes.Change.StatusMessage);
    }

    [AvaloniaFact]
    public async Task Apply_ProfileWithoutAnInstallFolder_SaysWhereToChooseOne()
    {
        var (_, viewModel) = await OpenFixesTabAsync();
        var profiles = _host.Resolve<IProfileRepository>();
        var profile = await profiles.LoadAsync("Default", TestContext.Current.CancellationToken);
        await profiles.SaveAsync(profile! with { InstallDirectory = null }, TestContext.Current.CancellationToken);

        await viewModel.Fixes.Change.ApplyLargeAddressAwareCommand.ExecuteAsync(null);

        Assert.True(viewModel.Fixes.Change.IsError);
        Assert.Contains("Choose it on the Profile page", viewModel.Fixes.Change.StatusMessage, StringComparison.Ordinal);
        Assert.EndsWith($"({DiagnosticCodes.FixInstallNotSet})", viewModel.Fixes.Change.StatusMessage, StringComparison.Ordinal);
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenFixesTabAsync()
    {
        _host.SeedRealisticInstall();
        _host.FileSystem.AddFile(Exe, new MockFileData(_original));
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.Profiles.NewProfileName = "Default";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);
        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = FixesTab;
        window.UpdateLayout();
        return (window, viewModel);
    }
}
