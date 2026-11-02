using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The window's six tabs (ADR-0026, P2.5-27): History on its own, and Settings holding the folders and Troubleshooting.</summary>
public sealed class SixTabsScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public SixTabsScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TheWindow_HasSixTabs_InOrder()
    {
        var (window, _) = await _scenario.OpenAsync();

        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        Assert.Equal(["Home", "Mods", "Fixes", "Saves", "History", "Settings"], tabs.Items.OfType<TabItem>().Select(t => t.Header as string));
        Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TabControl>(), t => t != tabs); // No tab inside another.
    }

    [AvaloniaFact]
    public async Task History_ShowsTheModsChanges_WithUndo()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);

        viewModel.SelectedPage = MainWindowViewModel.HistoryPage;
        window.UpdateLayout();

        Assert.Equal("Turned on Hair Pack", Assert.Single(viewModel.ModsTab.Changes.Backups).Change);
        Assert.Single(window.GetLogicalDescendants().OfType<ChangesPanel>().Distinct());
        Assert.Single(window.GetLogicalDescendants().OfType<FixHistoryPanel>().Distinct());
        Screenshots.Save(window, "33-history");
    }

    [AvaloniaFact]
    public async Task HomeButtons_OpenSettingsAtTheGameFoldersOrTroubleshooting()
    {
        var (window, viewModel) = await _scenario.OpenAsync();

        await viewModel.Home.OpenCommand.ExecuteAsync(HomeTarget.GameFolders);
        Assert.Equal(MainWindowViewModel.SettingsPage, viewModel.SelectedPage);
        Assert.True(viewModel.GameFoldersOpen);

        await viewModel.Home.OpenCommand.ExecuteAsync(HomeTarget.Diagnostics);
        Assert.True(viewModel.TroubleshootingOpen);
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs(); // The opened section scrolls itself into view.
        window.UpdateLayout();
        Screenshots.Save(window, "34-settings-sections");
    }
}
