using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Mods.Troubleshooting;
using Xunit;

namespace DAORework.App.Tests;

/// <summary><i>Find the problem mod</i> on a library the size of a real one (ADR-0042).</summary>
public sealed class ProblemSearchScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly RealScaleScenario _scenario;

    public ProblemSearchScreenTests()
    {
        _scenario = new RealScaleScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task AWholeSearch_FindsTheMod_ThenEverythingGoesBackWithoutIt()
    {
        var (_, viewModel) = await OpenWithNearlyEveryModOnAsync();
        var tab = viewModel.ModsTab;
        var search = tab.ProblemSearch;
        var before = tab.Activation.ActiveModIds.ToList();
        var culprit = before[^3];

        await search.StartCommand.ExecuteAsync(null);
        Assert.StartsWith("Round 1: turn off ", search.Preview.Title, StringComparison.Ordinal);
        Assert.False(search.IsTesting); // Nothing moves until Apply.
        await search.ApplyCommand.ExecuteAsync(null);

        var rounds = 1;
        while (search.IsTesting)
        {
            Assert.False(search.IsError, search.StatusMessage);
            var answer = tab.Activation.ActiveModIds.Contains(culprit) ? search.StillHappensCommand : search.ItsGoneCommand;
            await answer.ExecuteAsync(null);
            if (search.CanApply)
            {
                await search.ApplyCommand.ExecuteAsync(null);
            }

            rounds++;
        }

        Assert.True(search.Found);
        Assert.Equal(culprit, search.Search!.Culprit);
        Assert.InRange(rounds, 2, 8);
        Assert.StartsWith("It looks like ", search.Headline, StringComparison.Ordinal);

        await search.PutBackWithoutCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);

        Assert.False(search.IsError, search.StatusMessage);
        Assert.True(search.IsIdle);
        Assert.Equal(before.Where(id => id != culprit), tab.Activation.ActiveModIds);
    }

    [AvaloniaFact]
    public async Task Stop_PutsEverythingBack_InTheSameOrder()
    {
        var (_, viewModel) = await OpenWithNearlyEveryModOnAsync();
        var tab = viewModel.ModsTab;
        var search = tab.ProblemSearch;
        var before = tab.Activation.ActiveModIds.ToList();
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);
        Assert.True(tab.Activation.ActiveModIds.Count < before.Count);

        await search.PutBackCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);

        Assert.True(search.IsIdle);
        Assert.Equal(before, tab.Activation.ActiveModIds);
    }

    [AvaloniaFact]
    public async Task ARound_CanBeUndoneInHistory_LikeAnyChange()
    {
        var (_, viewModel) = await OpenWithNearlyEveryModOnAsync();
        var tab = viewModel.ModsTab;
        var backups = tab.Changes.Backups.Count;

        await tab.ProblemSearch.StartCommand.ExecuteAsync(null);
        await tab.ProblemSearch.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(backups + 1, tab.Changes.Backups.Count);
    }

    [AvaloniaFact]
    public async Task ASearch_IsStillThereAfterClosingTheApp()
    {
        var (window, viewModel) = await OpenWithNearlyEveryModOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);
        var saved = search.Search!;

        await search.LoadAsync(search.ProfileName, TestContext.Current.CancellationToken);

        Assert.True(search.IsTesting);
        Assert.Equal(saved.Off, search.Search!.Off);
        window.UpdateLayout();
        Assert.True(window.GetVisualDescendants().OfType<ProblemSearchPanel>().Single().IsEffectivelyVisible);
        Screenshots.Save(window, "real-scale-problem-search");
    }

    [AvaloniaFact]
    public async Task ACancelledRound_LeavesTheSearchWhereItWas()
    {
        var (_, viewModel) = await OpenWithNearlyEveryModOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);
        var round = search.Search!.Round;

        await search.StillHappensCommand.ExecuteAsync(null);
        search.CancelPlanCommand.Execute(null);

        Assert.Equal(round, search.Search!.Round);
        Assert.Null(search.Preview.Title);
    }

    [AvaloniaFact]
    public async Task Forget_EndsTheSearch_AndChangesNothing()
    {
        var (_, viewModel) = await OpenWithNearlyEveryModOnAsync();
        var tab = viewModel.ModsTab;
        var search = tab.ProblemSearch;
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);
        var during = tab.Activation.ActiveModIds.ToList();

        await search.ForgetCommand.ExecuteAsync(null);
        await search.LoadAsync(search.ProfileName, TestContext.Current.CancellationToken);

        Assert.True(search.IsIdle);
        Assert.Equal(during, tab.Activation.ActiveModIds);
    }

    [AvaloniaFact]
    public async Task EveryAnswerIsItStillHappens_EndsOnNotOneOfYourMods()
    {
        var (_, viewModel) = await OpenWithNearlyEveryModOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);

        while (search.IsTesting)
        {
            await search.StillHappensCommand.ExecuteAsync(null);
            if (search.CanApply)
            {
                await search.ApplyCommand.ExecuteAsync(null);
            }
        }

        Assert.Equal(ProblemSearchEnd.NotFound, search.Search!.End);
        Assert.Equal("It doesn't look like one of your mods", search.Headline);
        Assert.False(search.Found);
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithNearlyEveryModOnAsync()
    {
        var opened = await _scenario.OpenAsync();
        var activation = opened.ViewModel.ModsTab.Activation;
        await RealScaleScenario.TurnOnEverythingAsync(opened.ViewModel);
        await activation.LeaveOutCommand.ExecuteAsync(null);
        await activation.ApplyCommand.ExecuteAsync(null);
        return opened;
    }
}
