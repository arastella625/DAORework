using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The Mods tab as one page (ADR-0026, P2.5-26): the kind cards, the chips, Arrange and Combined files.</summary>
public sealed class ModsListScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public ModsListScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Chips_ShowAllOnOffOrFound()
    {
        var (window, viewModel) = await OpenWithModsAsync();
        var filter = viewModel.ModsTab.Filter;

        Assert.Equal(["Eyes", "Hair Pack"], filter.VisibleMods.Select(m => m.Title).Order());
        Assert.True(filter.ShowsMods && filter.ShowsFound);
        Assert.Equal("On (1)", filter.OnLabel);
        ModsTabScenario.Show<ModsListPanel>(window);
        Screenshots.Save(window, "32-mods-one-page");

        filter.ShowOnlyCommand.Execute(ModShowFilter.On);
        Assert.Equal("Hair Pack", Assert.Single(filter.VisibleMods).Title);
        Assert.False(filter.ShowsFound);

        filter.ShowOnlyCommand.Execute(ModShowFilter.Off);
        Assert.Equal("Eyes", Assert.Single(filter.VisibleMods).Title);

        filter.ShowOnlyCommand.Execute(ModShowFilter.Found);
        Assert.False(filter.ShowsMods);
        Assert.True(filter.ShowsFound);
        Assert.NotEmpty(filter.VisibleFound);
    }

    [AvaloniaFact]
    public async Task KindCards_NarrowThePage_AndTheSameCardShowsEveryKindAgain()
    {
        var (_, viewModel) = await OpenWithModsAsync();
        var filter = viewModel.ModsTab.Filter;

        filter.PickKindCommand.Execute(ModKindFilter.AddIns);

        Assert.Empty(filter.VisibleMods);
        Assert.Equal("Extra Dog Slot v1.41", Assert.Single(filter.VisibleFound).Title);
        Assert.Equal("Showing add-ins only.", filter.KindNote);

        filter.PickKindCommand.Execute(ModKindFilter.OverrideMods);
        Assert.Equal(2, filter.VisibleMods.Count);
        Assert.DoesNotContain(filter.VisibleFound, m => m.IsAddIn);

        filter.PickKindCommand.Execute(ModKindFilter.OverrideMods);
        Assert.Equal(ModKindFilter.All, filter.Kind);
        Assert.Null(filter.KindNote);
    }

    [AvaloniaFact]
    public async Task CombinedFilesCard_AndArrange_TakeThePlaceOfTheList()
    {
        var (_, viewModel) = await OpenWithModsAsync();
        var filter = viewModel.ModsTab.Filter;

        filter.PickKindCommand.Execute(ModKindFilter.CombinedFiles);
        Assert.True(filter.ShowsCombined);
        Assert.False(filter.ShowsList);

        filter.ToggleArrangeCommand.Execute(null);
        Assert.True(filter.Arranging);
        Assert.False(filter.ShowsCombined);
        Assert.Equal("Done arranging", filter.ArrangeLabel);

        filter.ShowOnlyCommand.Execute(ModShowFilter.All);
        Assert.False(filter.Arranging);
        Assert.True(filter.ShowsList); // A chip brings the list back from the Combined files card too.
    }

    // Two library mods, one of them on, and an add-in found in the game beside the realistic install's override folders.
    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithModsAsync()
    {
        var documents = GamePaths.DefaultDocumentsDirectory(_host.KnownFolders.MyDocuments);
        _host.FileSystem.AddFile(
            GamePaths.AddInsListFile(documents),
            new MockFileData("""<AddInsList><AddInItem UID="Extra_Dog_Slot"><Title DefaultText="Extra Dog Slot v1.41" /></AddInItem></AddInsList>"""));
        _host.FileSystem.AddFile(Path.Combine(GamePaths.AddInsDirectory(documents), "Extra_Dog_Slot", "core", "dog.gda"), new MockFileData(new byte[64]));
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ImportAsync(viewModel, MockUnixSupport.Path(@"C:\Users\Me\Downloads\Eyes.zip"), ("eyes.dds", "DDS eyes"));
        await _scenario.ActivateHairPackAsync(viewModel);
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
        return (window, viewModel);
    }
}
