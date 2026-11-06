using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The window opened on <see cref="RealScaleLibrary"/>, at its own size, with seven of its mods on.</summary>
internal sealed class RealScaleScenario(AppTestHost host)
{
    private readonly ModsTabScenario _scenario = new(host);

    /// <param name="withCopies">Whether the library holds <see cref="RealScaleLibrary.WithCopies"/>.</param>
    public async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync(bool withCopies = false)
    {
        var entries = withCopies ? RealScaleLibrary.WithCopies : RealScaleLibrary.Entries;
        host.SeedRealisticInstall();
        RealScaleLibrary.Seed(host, entries);
        var (window, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        await viewModel.Library.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(entries.Count - 1, viewModel.Library.Mods.Count); // __MACOSX has nothing for the game.
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
            mod.IsSelected = mod.Copy is not { Rank: > 0 } && picked(mod.Title);
        }
    }
}
