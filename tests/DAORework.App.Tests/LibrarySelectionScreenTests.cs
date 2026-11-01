using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Activating and deactivating several Library mods at once from their tick boxes (P2.5-14): one plan, one backup.</summary>
public sealed class LibrarySelectionScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");
    private readonly ModsTabScenario _scenario;

    public LibrarySelectionScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Ticks_EnableOnlyWhatTheTickedModsAllow()
    {
        var (_, viewModel) = await OpenWithThreeModsAsync();
        var selection = viewModel.ModsTab.Selection;

        Assert.False(selection.ActivateSelectedCommand.CanExecute(null));
        Assert.False(selection.DeactivateSelectedCommand.CanExecute(null));

        selection.SelectAllCommand.Execute(null);

        Assert.All(viewModel.Library.Mods, m => Assert.True(m.IsSelected));
        Assert.True(selection.ActivateSelectedCommand.CanExecute(null));
        Assert.False(selection.DeactivateSelectedCommand.CanExecute(null));

        selection.ClearSelectionCommand.Execute(null);

        Assert.All(viewModel.Library.Mods, m => Assert.False(m.IsSelected));
        Assert.False(selection.ActivateSelectedCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task ActivateSelected_ThenDeactivateSelected_EachIsOneChange()
    {
        var (window, viewModel) = await OpenWithThreeModsAsync();
        var selection = viewModel.ModsTab.Selection;
        var activation = viewModel.ModsTab.Activation;
        viewModel.Library.Mods.Single(m => m.Title == "Eyes").IsSelected = true;
        viewModel.Library.Mods.Single(m => m.Title == "Nails").IsSelected = true;

        await selection.ActivateSelectedCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Equal("Turn on 2 mods: Eyes, Nails", activation.PlanTitle);
        Assert.Contains("  Copy      2 files", activation.PlanLines);
        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "29-activate-selected");

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Equal(["Eyes", "Nails"], viewModel.Library.Mods.Where(m => m.IsActive).Select(m => m.Title).Order());
        Assert.Single(viewModel.ModsTab.Changes.Backups);

        selection.SelectAllCommand.Execute(null);

        Assert.True(selection.ActivateSelectedCommand.CanExecute(null));
        Assert.True(selection.DeactivateSelectedCommand.CanExecute(null));

        await selection.DeactivateSelectedCommand.ExecuteAsync(null);

        Assert.Equal("Turn off 2 mods: Eyes, Nails", activation.PlanTitle);
        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.DoesNotContain(viewModel.Library.Mods, m => m.IsActive);
        Assert.Equal(2, viewModel.ModsTab.Changes.Backups.Count);
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithThreeModsAsync()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Eyes.zip"), ("eyes.dds", "eyes"));
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Nails.zip"), ("nails.dds", "nails"));
        return (window, viewModel);
    }
}
