using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Installations;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Renders every page of the shell on the headless platform and saves the screenshots that go on the PR (SDD §9.2).</summary>
public sealed class MainWindowTests : IDisposable
{
    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task EmptyState_RendersWithNoProfiles()
    {
        var (window, viewModel) = await OpenAsync();

        Assert.Empty(viewModel.Profiles.Profiles);
        Assert.False(viewModel.Launch.LaunchCommand.CanExecute(null));
        var path = Screenshots.Save(window, "00-empty");
        Assert.True(File.Exists(path));
    }

    [AvaloniaFact]
    public async Task ProfilePage_ShowsThePathsOfTheSelectedProfile()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        await CreateAndPointAtInstallAsync(viewModel);

        Assert.Equal("Default", viewModel.Profiles.SelectedProfile);
        Assert.Equal(AppTestHost.InstallDirectory, viewModel.Profile.Paths.InstallDirectory);
        Assert.True(viewModel.Launch.LaunchCommand.CanExecute(null));

        await viewModel.Profile.Installs.DetectCommand.ExecuteAsync(null);

        var detected = Assert.Single(viewModel.Profile.Installs.Installs);
        Assert.Equal(Distribution.Steam, detected.Distribution);
        Assert.Equal(AppTestHost.InstallDirectory, detected.InstallDirectory);
        var documents = Assert.Single(viewModel.Profile.Installs.DocumentsFolders);
        Assert.True(documents.Exists);
        Screenshots.Save(window, "01-profile");
    }

    [AvaloniaFact]
    public async Task ProfilePage_UseDocumentsFolder_PointsTheProfileAtIt()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        viewModel.Profiles.NewProfileName = "Fresh";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Fresh", TestContext.Current.CancellationToken);
        await viewModel.Profile.Installs.DetectCommand.ExecuteAsync(null);
        var folder = viewModel.Profile.Installs.DocumentsFolders[0];

        await viewModel.Profile.Installs.UseDocumentsCommand.ExecuteAsync(folder);

        Assert.False(viewModel.Profile.Installs.IsError);
        Assert.Equal(folder.Path, viewModel.Profile.Paths.UserDocumentsDirectory);
        Screenshots.Save(window, "07-documents-folder");
    }

    [AvaloniaFact]
    public async Task ProfilePage_UseDetectedInstall_FillsThePaths()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        viewModel.Profiles.NewProfileName = "Fresh";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Fresh", TestContext.Current.CancellationToken);
        await viewModel.Profile.Installs.DetectCommand.ExecuteAsync(null);

        await viewModel.Profile.Installs.UseCommand.ExecuteAsync(viewModel.Profile.Installs.Installs[0]);

        Assert.False(viewModel.Profile.Installs.IsError);
        Assert.Equal(AppTestHost.InstallDirectory, viewModel.Profile.Paths.InstallDirectory);
        Assert.Equal(GamePaths.GameExecutable(AppTestHost.InstallDirectory), viewModel.Profile.Paths.GameExecutablePath);
        Assert.Equal(GamePaths.LauncherExecutable(AppTestHost.InstallDirectory), viewModel.Profile.Paths.LauncherExecutablePath);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(_host.ProfilesDirectory, "Fresh.json")));
        Screenshots.Save(window, "06-detected-install");
    }

    [AvaloniaFact]
    public async Task DiagnosticsPage_ListsTheIssuesAndTheReport()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        await CreateAndPointAtInstallAsync(viewModel);
        SelectTab(window, 1);

        await viewModel.Diagnostics.RunDiagnosticsCommand.ExecuteAsync(null);

        Assert.True(viewModel.Diagnostics.HasReport);
        Assert.True(viewModel.Diagnostics.CanLaunch);
        Assert.Contains(viewModel.Diagnostics.Issues, i => i.Code == DiagnosticCodes.ProfileValid);
        Assert.True(_host.FileSystem.File.Exists(viewModel.Diagnostics.ReportPath));
        Screenshots.Save(window, "02-diagnostics");
    }

    [AvaloniaFact]
    public async Task DiagnosticsPage_ExportSupportBundle_WritesTheZip()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        await CreateAndPointAtInstallAsync(viewModel);
        SelectTab(window, 1);

        await viewModel.Diagnostics.ExportSupportBundleCommand.ExecuteAsync(null);

        Assert.False(viewModel.Diagnostics.IsError);
        Assert.True(_host.FileSystem.File.Exists(viewModel.Diagnostics.BundlePath));
        Screenshots.Save(window, "08-support-bundle");
    }

    [AvaloniaFact]
    public async Task ModsPage_ShowsBothFoldersAndTheInventory()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        await CreateAndPointAtInstallAsync(viewModel);
        SelectTab(window, 2);

        await viewModel.Mods.InspectCommand.ExecuteAsync(null);
        viewModel.Mods.IncludeHashes = true;
        await viewModel.Mods.WriteInventoryCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Mods.Folders.Count);
        Assert.True(viewModel.Mods.Folders[0].Exists);
        Assert.Equal(4, viewModel.Mods.Folders[0].FileCount);
        Assert.NotNull(viewModel.Mods.Inventory);
        Assert.Equal(5, viewModel.Mods.Inventory.TotalFiles);
        Assert.Equal(["dupe.gda"], viewModel.Mods.Inventory.DuplicateFileNames);
        Assert.True(_host.FileSystem.File.Exists(viewModel.Mods.InventoryPath));
        ModsTabScenario.Show<ModsPage>(window);
        Screenshots.Save(window, "03-mods");
    }

    [AvaloniaFact]
    public async Task SavesPage_BackUpTwiceWithKeepOne_ShowsOneBackup()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        await CreateAndPointAtInstallAsync(viewModel);
        SelectTab(window, 3);
        viewModel.Saves.KeepLatest = 1;

        await viewModel.Saves.BackUpCommand.ExecuteAsync(null);
        await viewModel.Saves.BackUpCommand.ExecuteAsync(null);

        Assert.False(viewModel.Saves.IsError);
        Assert.Contains("Removed 1 older backup", viewModel.Saves.StatusMessage, StringComparison.Ordinal);
        Assert.Single(viewModel.Saves.Backups);
        Screenshots.Save(window, "09-saves");
    }

    [AvaloniaFact]
    public async Task Launch_SteamInstall_AsksSteamToStartTheGame()
    {
        _host.SeedRealisticInstall();
        var (window, viewModel) = await OpenAsync();
        await CreateAndPointAtInstallAsync(viewModel);

        await viewModel.Launch.LaunchCommand.ExecuteAsync(null);

        Assert.False(viewModel.Launch.IsError);
        Assert.Equal("Steam was asked to start the game. If Steam is closed, it opens first.", viewModel.Launch.StatusMessage);
        Assert.Equal("steam://rungameid/47810", _host.UriLauncher.LastUri?.AbsoluteUri);
        Assert.Null(_host.ProcessLauncher.LastRequest);
        Screenshots.Save(window, "04-launched");
    }

    /// <summary>Found by the owner on the first real run: a long error in the header pushed the buttons off-screen.</summary>
    [AvaloniaFact]
    public async Task InvalidProfileName_KeepsTheHeaderButtonsInsideTheWindow()
    {
        var (window, viewModel) = await OpenAsync();
        viewModel.Profiles.NewProfileName = @"D:\";

        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        window.UpdateLayout();

        Assert.True(viewModel.Profiles.IsError);
        var launch = window.FindControl<Button>("LaunchButton") ?? throw new InvalidOperationException("No LaunchButton.");
        var create = window.FindControl<Button>("CreateProfileButton") ?? throw new InvalidOperationException("No CreateProfileButton.");

        // Button bounds are relative to the header grid, which starts 16 px in (the border padding).
        Assert.True(launch.Bounds.Width > 0, "The launch button was not laid out.");
        Assert.True(launch.Bounds.Right + 16 <= window.ClientSize.Width, $"The launch button ends at {launch.Bounds.Right}, outside the {window.ClientSize.Width} px window.");
        Assert.True(create.Bounds.Right + 16 <= window.ClientSize.Width, $"The create button ends at {create.Bounds.Right}, outside the {window.ClientSize.Width} px window.");
        Screenshots.Save(window, "05-invalid-name");
    }

    private static void SelectTab(MainWindow window, int index)
    {
        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = index;
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        return (window, viewModel);
    }

    private async Task CreateAndPointAtInstallAsync(MainWindowViewModel viewModel)
    {
        viewModel.Profiles.NewProfileName = "Default";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);

        _host.Picker.Enqueue(AppTestHost.InstallDirectory);
        await viewModel.Profile.Paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Install);
        _host.Picker.Enqueue(GamePaths.GameExecutable(AppTestHost.InstallDirectory));
        await viewModel.Profile.Paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Game);
        _host.Picker.Enqueue(GamePaths.LauncherExecutable(AppTestHost.InstallDirectory));
        await viewModel.Profile.Paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Launcher);
    }
}
