using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The window opened on <see cref="RealScaleLibrary"/>, at a laptop's size, with seven of its mods on.</summary>
internal sealed class RealScaleScenario(AppTestHost host)
{
    private readonly ModsTabScenario _scenario = new(host);

    public async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync()
    {
        host.SeedRealisticInstall();
        RealScaleLibrary.Seed(host);
        var (window, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        window.Width = 1280;
        window.Height = 800;
        await viewModel.Library.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(RealScaleLibrary.Entries.Count, viewModel.Library.Mods.Count);
        Pick(viewModel, RealScaleLibrary.AlreadyOn.Contains);
        await viewModel.ModsTab.Selection.ActivateSelectedCommand.ExecuteAsync(null);
        await viewModel.ModsTab.Activation.ApplyCommand.ExecuteAsync(null);
        Assert.False(viewModel.ModsTab.Activation.IsError, viewModel.ModsTab.Activation.StatusMessage);
        Pick(viewModel, _ => false);
        return (window, viewModel);
    }

    /// <summary>Ticks every mod that is off and presses <i>Turn on selected</i>, as the player did.</summary>
    public static async Task TurnOnEverythingAsync(MainWindowViewModel viewModel)
    {
        Pick(viewModel, _ => true);
        await viewModel.ModsTab.Selection.ActivateSelectedCommand.ExecuteAsync(null);
        Assert.False(viewModel.ModsTab.Activation.IsError, viewModel.ModsTab.Activation.StatusMessage);
    }

    private static void Pick(MainWindowViewModel viewModel, Func<string, bool> picked)
    {
        foreach (var mod in viewModel.Library.Mods)
        {
            mod.IsSelected = picked(mod.Title);
        }
    }
}
