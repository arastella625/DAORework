using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>
/// What a real library showed (P2.6-61 to 63): the window called everything fine while it was still reading the mods, a second
/// copy could open, and <i>Find the problem mod</i> sat behind a card that can say <i>None needed</i>.
/// </summary>
public sealed class SharedCheckScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public SharedCheckScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task WhenTheGameReadsAnotherFolder_NothingSaysAllIsWell()
    {
        var (_, viewModel) = await OpenWithHairPackOnAsync();
        var setup = viewModel.ModsTab.Setup;
        Assert.Equal("Everything is in place", setup.Title);

        setup.GameReadsElsewhere = true; // What a check that found DAO_DOCUMENTS_NOT_THE_GAMES sets (ADR-0046).

        Assert.Equal("Almost there", setup.Title);
        Assert.Equal("Your mods are in a documents folder the game doesn't read. Worth a look says which.", setup.Body);

        setup.GameReadsElsewhere = false;

        Assert.Equal("Everything is in place", setup.Title);
    }

    [AvaloniaFact]
    public async Task WhileSharedFilesAreBeingRead_NothingSaysAllIsWell()
    {
        var (window, viewModel) = await OpenWithHairPackOnAsync();
        var tab = viewModel.ModsTab;
        Assert.Equal("Everything is in place", tab.Setup.Title);
        var gate = new TaskCompletionSource();
        tab.LoadOrder.BeforeWorkAsync = () => gate.Task;
        tab.Filter.Kind = ModKindFilter.CombinedFiles;

        var reading = tab.LoadOrder.RefreshAsync(TestContext.Current.CancellationToken);
        window.UpdateLayout();

        Assert.Equal("Checking your mods", tab.Setup.Title);
        Assert.Equal("Checking", Part(tab, "Override mods").Status);
        Assert.Equal("Not known yet", Part(tab, "Combined files").Count);
        Assert.All(tab.Setup.Parts.Skip(1), p => Assert.False(p.IsReady || p.NeedsALook));
        Assert.False(Named<TextBlock>(window, "NoSharedFiles").IsVisible);
        Assert.True(Named<TextBlock>(window, "SharedCheckNote").IsVisible);

        gate.SetResult();
        await reading;
        window.UpdateLayout();

        Assert.Equal("Everything is in place", tab.Setup.Title);
        Assert.Equal("None needed", Part(tab, "Combined files").Count);
        Assert.True(Named<TextBlock>(window, "NoSharedFiles").IsVisible);
        Assert.False(Named<TextBlock>(window, "SharedCheckNote").IsVisible);
    }

    [AvaloniaFact]
    public async Task ACheckThatFails_SaysSo_AndAGoodOneClearsIt()
    {
        var (window, viewModel) = await OpenWithHairPackOnAsync();
        var tab = viewModel.ModsTab;
        tab.Filter.Kind = ModKindFilter.CombinedFiles;
        var working = tab.LoadOrder.BeforeWorkAsync;
        tab.LoadOrder.BeforeWorkAsync = () => throw new IOException("The disk went away.");

        await tab.LoadOrder.RefreshAsync(TestContext.Current.CancellationToken);
        window.UpdateLayout();

        Assert.Equal("Almost there", tab.Setup.Title);
        Assert.Equal("Couldn't check", Part(tab, "Combined files").Status);
        Assert.True(Part(tab, "Combined files").NeedsALook);
        Assert.False(Named<TextBlock>(window, "NoSharedFiles").IsVisible);
        Assert.True(Named<TextBlock>(window, "SharedCheckError").IsVisible);

        tab.LoadOrder.BeforeWorkAsync = working;
        await tab.LoadOrder.RefreshAsync(TestContext.Current.CancellationToken);
        window.UpdateLayout();

        Assert.Equal("Everything is in place", tab.Setup.Title);
        Assert.False(Named<TextBlock>(window, "SharedCheckError").IsVisible);
    }

    [AvaloniaFact]
    public async Task WithNothingOn_ThereIsNothingToWaitFor()
    {
        var (_, viewModel) = await OpenWithNothingInTheGameAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var gate = new TaskCompletionSource();
        var tab = viewModel.ModsTab;
        tab.LoadOrder.BeforeWorkAsync = () => gate.Task;

        var reading = tab.LoadOrder.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Everything is in place", tab.Setup.Title);
        Assert.False(tab.Setup.HasModsOn);

        gate.SetResult();
        await reading;
    }

    [AvaloniaFact]
    public async Task TheModsTab_ReachesFindTheProblemMod_FromTheList()
    {
        var (window, viewModel) = await OpenWithHairPackOnAsync();
        var button = Named<Button>(window, "FindProblemModButton");
        Assert.True(button.IsVisible);

        button.Command!.Execute(button.CommandParameter);
        window.UpdateLayout();

        Assert.True(viewModel.ModsTab.Filter.ShowsCombined);
        Assert.True(Named<ProblemSearchPanel>(window, null).IsVisible);
        Assert.False(button.IsVisible);
    }

    [AvaloniaFact]
    public async Task Home_ReachesFindTheProblemMod_OnceAModIsOn()
    {
        var (window, viewModel) = await OpenWithNothingInTheGameAsync();
        viewModel.SelectedPage = MainWindowViewModel.HomePage;
        window.UpdateLayout();
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(), b => b.Name == "FindProblemButton" && b.IsEffectivelyVisible);

        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);
        viewModel.SelectedPage = MainWindowViewModel.HomePage;
        window.UpdateLayout();
        var button = Named<Button>(window, "FindProblemButton");
        Assert.True(button.IsEffectivelyVisible);

        await viewModel.Home.OpenCommand.ExecuteAsync((HomeTarget)button.CommandParameter!);

        Assert.Equal(MainWindowViewModel.ModsPage, viewModel.SelectedPage);
        Assert.True(viewModel.ModsTab.Filter.ShowsCombined);
    }

    [AvaloniaFact]
    public async Task Home_OpensTheModsTabAtTheTop_NotWhereItWasLeft()
    {
        var (window, viewModel) = await new RealScaleScenario(_host).OpenAsync();
        var page = window.GetVisualDescendants().OfType<ModsListPanel>().First().FindAncestorOfType<ScrollViewer>()!;
        page.Offset = new Vector(0, 3000);
        window.UpdateLayout();
        Assert.True(page.Offset.Y > 1000);

        viewModel.SelectedPage = MainWindowViewModel.HomePage;
        window.UpdateLayout();
        await viewModel.Home.OpenCommand.ExecuteAsync(HomeTarget.ModsLibrary); // The long list, where the place really is kept.
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.True(page.Offset.Y < 200, $"Still {page.Offset.Y} down the page.");
    }

    [AvaloniaFact]
    public void AnotherCopyOpen_SaysSo_AndCloses()
    {
        var window = new AlreadyOpenWindow();
        window.Show();
        var close = Named<Button>(window, "AlreadyOpenClose");

        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.False(window.IsVisible);
    }

    private static SetupPart Part(ModsTabViewModel tab, string title) => tab.Setup.Parts.Single(p => p.Title == title);

    private static T Named<T>(Window window, string? name)
        where T : Control =>
        window.GetVisualDescendants().OfType<T>().First(c => name is null || c.Name == name);

    /// <summary>The realistic install has mods put in by hand, which alone would keep Home from saying all is well.</summary>
    private Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithNothingInTheGameAsync()
    {
        _host.SeedRealisticInstall();
        _host.FileSystem.Directory.Delete(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), recursive: true);
        return _scenario.OpenAsync(seedInstall: false);
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithHairPackOnAsync()
    {
        var opened = await OpenWithNothingInTheGameAsync();
        await _scenario.ImportHairPackAsync(opened.ViewModel);
        await _scenario.ActivateHairPackAsync(opened.ViewModel);
        return opened;
    }
}
