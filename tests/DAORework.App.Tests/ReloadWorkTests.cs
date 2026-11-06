using Avalonia.Headless.XUnit;
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

        Assert.Equal(1, listChanges);
        Assert.InRange(nextStepChanges, 0, 12);
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
