using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Mods;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The one next step leads the player through (P2.6-27).</summary>
public sealed class NextMoveScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public NextMoveScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ModsFoundInTheGame_TakeThemOver_OpensOnePreview_ThenTheStepMovesOn()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var card = viewModel.ModsTab.NextMove;
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
        Assert.Equal(NextMoveKind.TakeOverWhatIsInTheGame, card.Kind);
        ModsTabScenario.Show<HandInstalledPanel>(window);
        Screenshots.Save(window, "next-move");

        await card.TakeCommand.ExecuteAsync(null);

        var adoption = viewModel.HandInstalled.Adoption;
        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.StartsWith("Manage ", adoption.PlanTitle, StringComparison.Ordinal);
        Assert.Equal(MainWindowViewModel.ModsPage, viewModel.SelectedPage);

        await adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(adoption.IsError, adoption.StatusMessage);

        // The game held dupe.gda in two places, so taking over leads straight to clearing the copy.
        Assert.Equal(NextMoveKind.ClearCopies, card.Kind);

        await card.TakeCommand.ExecuteAsync(null);
        await viewModel.ModsTab.KeepEach.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(viewModel.ModsTab.KeepEach.IsError, viewModel.ModsTab.KeepEach.StatusMessage);
        Assert.Equal(NextMoveKind.Play, card.Kind);
        Assert.False(card.IsShown);
    }

    [AvaloniaFact]
    public async Task ANewPlayer_IsLedToAddMods_ThenToTurnThemOn()
    {
        var (window, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        var card = viewModel.ModsTab.NextMove;

        Assert.Equal(NextMoveKind.AddMods, card.Kind);
        Assert.Equal([true, false, false], card.FirstSteps.Select(s => s.IsDone));
        viewModel.SelectedPage = MainWindowViewModel.HomePage;
        window.UpdateLayout();
        Screenshots.Save(window, "first-steps");

        await _scenario.ImportHairPackAsync(viewModel);

        Assert.Equal(NextMoveKind.TurnOnMods, card.Kind);
        Assert.Equal([true, true, false], card.FirstSteps.Select(s => s.IsDone));

        await _scenario.ActivateHairPackAsync(viewModel);

        Assert.Equal(NextMoveKind.Play, card.Kind);
        Assert.Empty(card.FirstSteps);
    }
}
