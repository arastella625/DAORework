using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Troubleshooting;
using Xunit;

namespace DAORework.App.Tests;

/// <summary><i>Find the problem mod</i> (ADR-0042). A whole search runs at real size; the rest need only a few mods.</summary>
public sealed class ProblemSearchScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _small;

    public ProblemSearchScreenTests()
    {
        _small = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task AWholeSearch_AtRealSize_FindsTheMod_ThenEverythingGoesBackWithoutIt()
    {
        var (_, viewModel) = await new RealScaleScenario(_host).OpenAsync();
        var tab = viewModel.ModsTab;
        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        await tab.Activation.LeaveOutCommand.ExecuteAsync(null);
        await tab.Activation.ApplyCommand.ExecuteAsync(null);
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
            Assert.True(search.CanAnswer, $"Round {search.Search!.Round} reads as out of step."); // P2.6-69.
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
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var tab = viewModel.ModsTab;
        var search = tab.ProblemSearch;
        var before = tab.Activation.ActiveModIds.ToList();
        await StartAsync(search);
        Assert.True(tab.Activation.ActiveModIds.Count < before.Count);

        await search.PutBackCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);

        Assert.True(search.IsIdle);
        Assert.Equal(before, tab.Activation.ActiveModIds);
    }

    [AvaloniaFact]
    public async Task ARound_IsAChangeInHistory_LikeAnyOther()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var tab = viewModel.ModsTab;
        var backups = tab.Changes.Backups.Count;

        await StartAsync(tab.ProblemSearch);

        Assert.Equal(backups + 1, tab.Changes.Backups.Count);
    }

    [AvaloniaFact]
    public async Task ASearch_IsStillThereAfterClosingTheApp_AndShowsWhateverIsPicked()
    {
        var (window, viewModel) = await OpenWithFourModsOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await StartAsync(search);
        var saved = search.Search!;

        await search.LoadAsync(search.ProfileName, TestContext.Current.CancellationToken);

        Assert.True(search.IsTesting);
        Assert.Equal(saved.Off, search.Search!.Off);
        window.UpdateLayout();
        Assert.True(window.GetVisualDescendants().OfType<ProblemSearchPanel>().Single().IsEffectivelyVisible);
        Screenshots.Save(window, "problem-search-round");
    }

    [AvaloniaFact]
    public async Task ACancelledRound_LeavesTheSearchWhereItWas()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await StartAsync(search);
        var round = search.Search!.Round;

        await search.StillHappensCommand.ExecuteAsync(null);
        search.CancelPlanCommand.Execute(null);

        Assert.Equal(round, search.Search!.Round);
        Assert.Null(search.Preview.Title);
    }

    [AvaloniaFact]
    public async Task ARound_SaysWhatEachAnswerMeans_InThePlayersWords()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        search.Problem = "the game crashes in Denerim";

        await StartAsync(search);

        Assert.Equal("You said the problem is: the game crashes in Denerim", search.YouSaid);
        Assert.Equal("Round 1 of about 2: 4 mods could be the one", search.Headline);
        Assert.Equal("Yes, still happening. The problem is in the 2 still on", search.StillHappensLabel);
        Assert.Equal("No, it's gone. The problem is in the 2 we turned off", search.ItsGoneLabel);
        Assert.Equal("0 ruled out, 4 still suspects", search.Progress);
    }

    [AvaloniaFact]
    public async Task KeepMyLooksOn_LeavesModsThatOnlyChangeLooksOutOfIt()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var tab = viewModel.ModsTab;
        var search = tab.ProblemSearch;
        var looks = tab.Library.Mods.Single(m => m.Title == "Pretty").Id;
        search.KeepLooksOn = true;

        await StartAsync(search);

        Assert.DoesNotContain(looks, search.Search!.Suspects);
        Assert.Contains(looks, tab.Activation.ActiveModIds);
    }

    [AvaloniaFact]
    public async Task ThePlainGameCheck_ProblemStillThere_EndsOnItNotBeingYourMods()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var tab = viewModel.ModsTab;
        var search = tab.ProblemSearch;

        await search.StartPlainCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);

        Assert.Empty(tab.Activation.ActiveModIds);

        await search.StillHappensCommand.ExecuteAsync(null);

        Assert.Equal("It doesn't look like your mods", search.Headline);
        Assert.True(search.IsFinished);
    }

    [AvaloniaFact]
    public async Task ThePlainGameCheck_ProblemGone_StartsTheRounds()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await search.StartPlainCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);

        await search.ItsGoneCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);

        Assert.StartsWith("Round 1 of about ", search.Headline, StringComparison.Ordinal);
        Assert.True(search.Search!.Proven);
    }

    [AvaloniaFact]
    public async Task Forget_EndsTheSearch_AndChangesNothing()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var tab = viewModel.ModsTab;
        var search = tab.ProblemSearch;
        await StartAsync(search);
        var during = tab.Activation.ActiveModIds.ToList();

        await search.ForgetCommand.ExecuteAsync(null);
        await search.LoadAsync(search.ProfileName, TestContext.Current.CancellationToken);

        Assert.True(search.IsIdle);
        Assert.Equal(during, tab.Activation.ActiveModIds);
    }

    [AvaloniaFact]
    public async Task EveryAnswerIsItStillHappens_EndsOnNotOneOfYourMods()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await StartAsync(search);

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

    private static async Task StartAsync(ProblemSearchViewModel search)
    {
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithFourModsOnAsync()
    {
        var opened = await _small.OpenAsync();
        var downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");
        foreach (var (name, file) in new[] { ("Pretty", "pretty.dds"), ("Rules", "rules.gda"), ("Talk", "talk.ncs"), ("Skills", "skills.gda") })
        {
            await _small.ImportAsync(opened.ViewModel, Path.Combine(downloads, name + ".zip"), (file, name));
        }

        var activation = opened.ViewModel.ModsTab.Activation;
        await activation.PlanChangeAsync(ModChangeKind.Activate, [.. opened.ViewModel.Library.Mods], TestContext.Current.CancellationToken);
        await activation.ApplyCommand.ExecuteAsync(null);
        return opened;
    }
}
