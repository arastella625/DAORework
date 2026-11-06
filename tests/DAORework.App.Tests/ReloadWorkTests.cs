using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>
/// How much work reading the mods again does at real scale (P2.6-44). These count changes, not time: a count means the same
/// on every machine, and each change makes the window lay out and work everything out again.
/// </summary>
public sealed class ReloadWorkTests : IDisposable
{
    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ReadingTheLibraryAgain_ChangesTheListOnce()
    {
        var (_, viewModel) = await new RealScaleScenario(_host).OpenAsync(withCopies: true);
        var tab = viewModel.ModsTab;
        var listChanges = 0;
        var nextStepChanges = 0;
        tab.Filter.VisibleMods.CollectionChanged += (_, _) => listChanges++;
        tab.NextMove.PropertyChanged += (_, _) => nextStepChanges++;

        await viewModel.Library.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, listChanges);
        Assert.InRange(nextStepChanges, 0, 12);
    }

    [AvaloniaFact]
    public async Task ALongList_BuildsOnlyTheRowsOnScreen_AndScrollingReachesTheLast()
    {
        var (window, viewModel) = await new RealScaleScenario(_host).OpenAsync(withCopies: true);
        ModsTabScenario.Show<LibraryPanel>(window);
        window.UpdateLayout();
        var list = window.GetLogicalDescendants().OfType<ItemsControl>().First(c => c.Name == "LibraryMods");
        var total = viewModel.ModsTab.Filter.VisibleMods.Count;

        Assert.InRange(list.GetRealizedContainers().Count(), 1, total / 2);

        // A page at a time, as a player scrolls: what follows the list means its last row isn't at the very end.
        var scroller = list.GetVisualAncestors().OfType<ScrollViewer>().First();
        var seen = new HashSet<int>();
        for (var page = 0; page < 100 && seen.Count < total; page++)
        {
            seen.UnionWith(Enumerable.Range(0, total).Where(i => list.ContainerFromIndex(i) is not null));
            scroller.Offset = scroller.Offset.WithY(scroller.Offset.Y + (scroller.Viewport.Height / 2));
            window.UpdateLayout();
        }

        Assert.Equal(total, seen.Count);
    }

    [AvaloniaFact]
    public async Task ReadingTheLibraryAgain_KeepsRowsThatSayTheSame_AndStartsThemFresh()
    {
        var (_, viewModel) = await new RealScaleScenario(_host).OpenAsync(withCopies: true);
        var first = viewModel.Library.Mods[0];
        first.IsExpanded = true;
        first.IsSelected = true;
        var listChanges = 0;
        viewModel.ModsTab.Filter.VisibleMods.CollectionChanged += (_, _) => listChanges++;

        await viewModel.Library.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Same(first, viewModel.Library.Mods[0]);
        Assert.False(first.IsExpanded);
        Assert.False(first.IsSelected);
        Assert.Equal(0, listChanges);
    }

    [AvaloniaFact]
    public async Task ReadingTheLibraryAgain_StillUpdatesTheCardsWhenEveryRowIsKept()
    {
        var (_, viewModel) = await new RealScaleScenario(_host).OpenAsync(withCopies: true);
        var heard = 0;
        viewModel.Library.Mods.CollectionChanged += (_, _) => heard++;

        await viewModel.Library.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, heard);
    }

    [AvaloniaFact]
    public async Task AKeptRow_StillUpdatesTheCountsOnce()
    {
        var (_, viewModel) = await new RealScaleScenario(_host).OpenAsync(withCopies: true);
        await viewModel.Library.LoadAsync(TestContext.Current.CancellationToken);
        var filter = viewModel.ModsTab.Filter;
        var row = viewModel.Library.Mods.First(m => !m.IsActive);
        var labelChanges = 0;
        filter.PropertyChanged += (_, e) => labelChanges += e.PropertyName == nameof(filter.OnLabel) ? 1 : 0;

        row.IsActive = true;

        Assert.Equal(1, labelChanges);
    }

    [AvaloniaFact]
    public async Task AMarkOnARow_DoesNotBuildTheListAgain()
    {
        var (_, viewModel) = await new RealScaleScenario(_host).OpenAsync(withCopies: true);
        var listChanges = 0;
        viewModel.ModsTab.Filter.VisibleMods.CollectionChanged += (_, _) => listChanges++;

        viewModel.Library.ShowActive(viewModel.Library.Mods.Select(m => m.Id));

        Assert.Equal(0, listChanges);
        Assert.All(viewModel.Library.Mods, m => Assert.True(m.IsActive));
    }

    [AvaloniaFact]
    public async Task ReadingWhatIsInTheGameAgain_ChangesTheFoundListOnce()
    {
        var (_, viewModel) = await new RealScaleScenario(_host).OpenAsync(withCopies: true);
        var handInstalled = viewModel.HandInstalled;
        var changes = 0;
        handInstalled.Candidates.CollectionChanged += (_, _) => changes++;

        await handInstalled.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.InRange(changes, 1, 2 + handInstalled.Candidates.Count(c => c.IsInGameFolder));
    }
}
