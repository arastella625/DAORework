using System.IO.Abstractions.TestingHelpers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The Mods tab's <i>Installed by hand</i> list (P2.5-02, ADR-0015), over the real scan and the catalog that ships.</summary>
public sealed class HandInstalledScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ModsTab_ListsWhatCanBeAdoptedAndNeverOfficialDlc()
    {
        _host.SeedRealisticInstall();
        var documents = GamePaths.DefaultDocumentsDirectory(_host.KnownFolders.MyDocuments);
        _host.FileSystem.AddFile(
            GamePaths.AddInsListFile(documents),
            new MockFileData("""
                <AddInsList>
                  <AddInItem UID="DAO_PRC_EP_1" BioWare="1"><Title DefaultText="Dragon Age: Awakening" /></AddInItem>
                  <AddInItem UID="Extra_Dog_Slot"><Title DefaultText="Extra Dog Slot v1.41" /></AddInItem>
                </AddInsList>
                """));
        _host.FileSystem.AddFile(Path.Combine(GamePaths.AddInsDirectory(documents), "Extra_Dog_Slot", "core", "dog.gda"), new MockFileData(new byte[1024]));
        _host.FileSystem.AddFile(Path.Combine(GamePaths.AddInsDirectory(documents), "Leftover_Mod", "x.erf"), new MockFileData(new byte[16]));

        // The list fills in when the profile is chosen, without Look again (P2.5-16).
        var (window, viewModel) = await OpenModsTabAsync();

        Assert.False(viewModel.HandInstalled.IsError, viewModel.HandInstalled.StatusMessage);
        Assert.Equal(
            ["Extra Dog Slot v1.41", "Loose files in override", "other", "qwinn_fixpack", "textures"],
            viewModel.HandInstalled.Candidates.Select(c => c.Title));
        Assert.Contains("Known as Extra Dog Slot: https://www.nexusmods.com/dragonage/mods/664", viewModel.HandInstalled.Candidates[0].Notes);
        var leftover = Assert.Single(viewModel.HandInstalled.NotAdoptable);
        Assert.Equal("Can't be managed here", leftover.Details);
        Assert.Equal("Official DLC (1 add-in) is left as it is.", viewModel.HandInstalled.OfficialSummary);
        Assert.DoesNotContain(viewModel.HandInstalled.Candidates, c => c.Details.Contains("DAO_PRC_EP_1", StringComparison.OrdinalIgnoreCase));
        ShowPanel(window);
        Screenshots.Save(window, "12-installed-by-hand");
    }

    [AvaloniaFact]
    public async Task ModsTab_VortexMods_AreNamed_AndOnlyTheOnesVortexLeftBehindOfferManage()
    {
        _host.SeedRealisticInstall();
        var documents = GamePaths.DefaultDocumentsDirectory(_host.KnownFolders.MyDocuments);
        var overrideFolder = Path.Combine(documents, "packages", "core", "override");
        var staging = Path.Combine(Path.GetDirectoryName(_host.PreferencesFile)!, "Vortex", "dragonage", "mods");
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "next part", "uh_hed_s76_0n.dds"), new MockFileData(new byte[2048]));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "uh_hed_s80_0d.dds"), new MockFileData(new byte[2048]));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "next part", "eyes.dds"), new MockFileData(new byte[512]));
        _host.FileSystem.AddDirectory(Path.Combine(staging, "EyesssReplaceDAO by ViLiSSa-3537-1-0"));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "vortex.deployment.json"), new MockFileData(System.Text.Json.JsonSerializer.Serialize(new
        {
            targetPath = overrideFolder,
            stagingPath = staging,
            files = new[]
            {
                new { relPath = "next part\\uh_hed_s76_0n.dds", source = "Anto Hairstyles 2-3480-1-2-3" },
                new { relPath = "uh_hed_s80_0d.dds", source = "Anto Hairstyles 2-3480-1-2-3" },
                new { relPath = "next part\\eyes.dds", source = "EyesssReplaceDAO by ViLiSSa-3537-1-0" },
            },
        })));

        var (window, viewModel) = await OpenModsTabAsync();

        Assert.False(viewModel.HandInstalled.IsError, viewModel.HandInstalled.StatusMessage);
        var hairs = viewModel.HandInstalled.Candidates.Single(c => c.Title == "Anto Hairstyles 2");
        Assert.StartsWith("placed by Vortex · 2 files", hairs.Details, StringComparison.Ordinal);
        Assert.True(hairs.CanManage);
        Assert.Contains(hairs.Notes, n => n.StartsWith("Vortex placed this mod but can no longer change it", StringComparison.Ordinal));
        var eyes = viewModel.HandInstalled.Candidates.Single(c => c.Title == "EyesssReplaceDAO by ViLiSSa");
        Assert.False(eyes.CanManage);
        Assert.DoesNotContain(viewModel.HandInstalled.Candidates, c => c.Title == "next part");
        viewModel.HandInstalled.SelectAllCommand.Execute(null);
        Assert.False(eyes.IsSelected);
        viewModel.HandInstalled.ClearSelectionCommand.Execute(null);
        ShowPanel(window);
        window.Height = 1040; // Down to the two Vortex mods, which come after the folders.
        window.UpdateLayout();
        Screenshots.Save(window, "35-vortex-mods");
    }

    [AvaloniaFact]
    public async Task ModsTab_ProfileWithoutADocumentsFolder_ShowsAnEmptyListAndNoError()
    {
        _host.SeedRealisticInstall();
        var (_, viewModel) = await OpenModsTabAsync();
        var profiles = _host.Resolve<DAORework.Application.Profiles.IProfileRepository>();
        var profile = await profiles.LoadAsync("Default", TestContext.Current.CancellationToken);
        await profiles.SaveAsync(profile! with { UserDocumentsDirectory = null }, TestContext.Current.CancellationToken);

        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);

        Assert.False(viewModel.HandInstalled.IsError, viewModel.HandInstalled.StatusMessage);
        Assert.Empty(viewModel.HandInstalled.Candidates);

        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);

        Assert.True(viewModel.HandInstalled.IsError);
        Assert.Contains("has no documents folder", viewModel.HandInstalled.StatusMessage, StringComparison.Ordinal);
    }

    /// <summary>Open the panel's section and scroll so its heading is at the top of the screenshot.</summary>
    private static void ShowPanel(MainWindow window) => ModsTabScenario.Show<HandInstalledPanel>(window);

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenModsTabAsync()
    {
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        viewModel.Profiles.NewProfileName = "Default";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);

        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = MainWindowViewModel.ModsPage;
        return (window, viewModel);
    }
}
