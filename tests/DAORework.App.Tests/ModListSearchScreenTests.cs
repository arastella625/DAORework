using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Finding a mod in a long list, and acting only on what is shown (P2.6-30).</summary>
public sealed class ModListSearchScreenTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public ModListSearchScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Search_NarrowsTheList_AndSelectAllTicksOnlyWhatIsShown()
    {
        var (window, viewModel) = await OpenWithThreeModsAsync();
        var filter = viewModel.ModsTab.Filter;

        filter.SearchText = "EYE";

        Assert.Equal("Eyes", Assert.Single(filter.VisibleMods).Title);
        Assert.False(filter.NothingMatches);
        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "mods-search");

        viewModel.ModsTab.Selection.SelectAllCommand.Execute(null);

        Assert.Equal("Eyes", Assert.Single(viewModel.Library.Mods, m => m.IsSelected).Title);

        filter.SearchText = "nothing like this";

        Assert.Empty(filter.VisibleMods);
        Assert.True(filter.NothingMatches);

        filter.SearchText = string.Empty;

        Assert.Equal(3, filter.VisibleMods.Count);
    }

    [AvaloniaFact]
    public async Task Sort_OnFirst_PutsTheModsThatAreOnAtTheTop()
    {
        var (_, viewModel) = await OpenWithThreeModsAsync();
        await _scenario.ActivateAsync(viewModel, "Nails");
        var filter = viewModel.ModsTab.Filter;

        filter.SortIndex = (int)ModSort.OnFirst;

        Assert.Equal("Nails", filter.VisibleMods[0].Title);
    }

    [Fact]
    public void Sorted_Newest_PutsTheLastAddedFirst()
    {
        var older = Row("older", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var newer = Row("newer", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(["newer", "older"], ModListOrder.Sorted([older, newer], ModSort.Newest).Select(r => r.Id));
    }

    private static LibraryModRow Row(string id, DateTimeOffset added) =>
        LibraryModRow.From(new LibraryEntry(new LibraryMod(id, id, ModKind.Override, []), id, id, added), Result.Failure<DownloadCheck>(new Error("X", "x")));

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithThreeModsAsync()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Eyes.zip"), ("eyes.dds", "eyes"));
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Nails.zip"), ("nails.dds", "nails"));
        return (window, viewModel);
    }
}
