using System.IO.Abstractions.TestingHelpers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>What the second test on the owner's real library found (P2.6-68 to 70).</summary>
public sealed class SecondRealTestScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _small;

    public SecondRealTestScreenTests()
    {
        _small = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ComingBackToTheWindow_DoesNotScrollToAControlScrolledAway()
    {
        var (window, viewModel) = await new RealScaleScenario(_host).OpenAsync();
        await viewModel.Home.OpenCommand.ExecuteAsync(HomeTarget.ModsCombined);
        Settle(window);
        var page = window.GetVisualDescendants().OfType<ModsListPanel>().First().FindAncestorOfType<ScrollViewer>()!;
        Assert.True(window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "ShowEveryKindButton").Focus());
        page.Offset = default;
        Settle(window);

        LeaveAndComeBack(window);

        Assert.True(page.Offset.Y < 1, $"Moved {page.Offset.Y} down the page.");
    }

    [AvaloniaFact]
    public async Task ComingBackToTheWindow_KeepsFocusOnAControlInSight()
    {
        var (window, viewModel) = await new RealScaleScenario(_host).OpenAsync();
        await viewModel.Home.OpenCommand.ExecuteAsync(HomeTarget.ModsCombined);
        Settle(window);
        var start = window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "StartSearchButton");
        Assert.True(start.Focus());

        LeaveAndComeBack(window);

        Assert.Same(start, TopLevel.GetTopLevel(window)!.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public async Task Tab_StillScrollsTheFocusIntoView()
    {
        var (window, viewModel) = await new RealScaleScenario(_host).OpenAsync();
        await viewModel.Home.OpenCommand.ExecuteAsync(HomeTarget.ModsCombined);
        Settle(window);
        var page = window.GetVisualDescendants().OfType<ModsListPanel>().First().FindAncestorOfType<ScrollViewer>()!;
        var buttons = page.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible && b.Focusable).ToList();
        Assert.True(buttons[^2].Focus());
        page.Offset = default;
        Settle(window);

        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(window);

        Assert.True(page.Offset.Y > 1, "Tab moved focus out of sight without scrolling.");
    }

    [AvaloniaFact]
    public async Task UndoDuringASearch_StopsTheAnswers_UntilTheRoundIsBack()
    {
        var (window, viewModel) = await OpenWithFourModsOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);
        viewModel.SelectedPage = MainWindowViewModel.ModsPage;
        Settle(window);
        var answer = window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "StillHappensButton");
        var putBack = window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PutRoundBackButton");
        Assert.True(answer.IsEffectivelyVisible);

        var changes = viewModel.ModsTab.Changes;
        await changes.RollBackToCommand.ExecuteAsync(changes.Backups[0]);
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);
        Assert.False(changes.IsError, changes.StatusMessage);

        viewModel.SelectedPage = MainWindowViewModel.ModsPage;
        Settle(window);
        Assert.False(answer.IsEffectivelyVisible);
        Assert.True(putBack.IsEffectivelyVisible);

        await search.PutRoundBackCommand.ExecuteAsync(null);
        Assert.StartsWith("Round 1: turn off ", search.Preview.Title, StringComparison.Ordinal);
        await search.ApplyCommand.ExecuteAsync(null);

        Settle(window);
        Assert.True(answer.IsEffectivelyVisible, search.StatusMessage);
        Assert.False(putBack.IsEffectivelyVisible);
        Assert.Equal(1, search.Search!.Round);
    }

    [AvaloniaFact]
    public async Task AnUndoneChange_LeavesHistory_InsteadOfSayingItChangedNothing()
    {
        var (_, viewModel) = await OpenWithFourModsOnAsync();
        var search = viewModel.ModsTab.ProblemSearch;
        await search.StartCommand.ExecuteAsync(null);
        await search.ApplyCommand.ExecuteAsync(null);
        var changes = viewModel.ModsTab.Changes;
        Assert.StartsWith("Turned off ", changes.Backups[0].Change, StringComparison.Ordinal);
        var rows = changes.Backups.Count;

        await changes.RollBackToCommand.ExecuteAsync(changes.Backups[0]);
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.Equal(rows - 1, changes.Backups.Count);
        Assert.DoesNotContain(changes.Backups, b => b.Change is "Changed no mods");
        Assert.StartsWith("Turned on ", changes.Backups[0].Change, StringComparison.Ordinal);
    }

    /// <summary>The window loses the front and comes back, which gives focus back as switching apps does.</summary>
    private static void LeaveAndComeBack(Window window)
    {
        window.Hide();
        window.Show();
        window.Activate();
        Settle(window);
    }

    [AvaloniaFact]
    public async Task UndoingBackToTheStart_LeavesNothingInHistory()
    {
        var (_, viewModel) = await _small.OpenAsync();
        await _small.ImportHairPackAsync(viewModel);
        await _small.ActivateHairPackAsync(viewModel);
        var changes = viewModel.ModsTab.Changes;

        await changes.RollBackToCommand.ExecuteAsync(Assert.Single(changes.Backups));
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.False(changes.IsError, changes.StatusMessage);
        Assert.Empty(changes.Backups);
    }

    private static void Settle(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
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
