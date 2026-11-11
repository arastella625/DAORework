using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Profiles;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Home, the first tab (ADR-0026, P2.5-25): ready or not, the setup at a glance, and the last change with Undo.</summary>
public sealed class HomeScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public HomeScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public void NoProfile_SaysHowToStart_AndOffersNoCheck()
    {
        var window = _host.Resolve<MainWindowViewModel>();
        var home = window.Home;

        Assert.Equal(MainWindowViewModel.HomePage, window.SelectedPage); // Home opens first.
        Assert.Equal("Welcome", home.ReadyTitle);
        Assert.False(home.CanCheck);
        Assert.False(home.HasLastChange);
    }

    [AvaloniaFact]
    public async Task ACheckedGame_IsReadyToPlay()
    {
        var (_, viewModel) = await _scenario.OpenAsync();

        await viewModel.Home.CheckGameCommand.ExecuteAsync(null);

        Assert.Equal("Ready to play", viewModel.Home.ReadyTitle);
        Assert.True(viewModel.Home.IsReady);
        Assert.False(viewModel.Home.CanCheck);
    }

    [AvaloniaFact]
    public async Task TurningOnAMod_ShowsInTheStrip_AndUndoFromHomeAsksInHistory()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);

        await _scenario.ActivateHairPackAsync(viewModel);

        var home = viewModel.Home;
        var overrides = Assert.Single(home.Setup!.Parts, p => p.Title == "Override mods");
        Assert.StartsWith("1 on", overrides.Count, StringComparison.Ordinal);
        Assert.Equal("Almost there", home.Setup.Title);
        Assert.Equal("Turned on Hair Pack", home.LastChange);
        viewModel.SelectedPage = MainWindowViewModel.HomePage;
        window.UpdateLayout();
        Screenshots.Save(window, "31-home");

        await home.UndoLastCommand.ExecuteAsync(null);

        Assert.Equal(MainWindowViewModel.HistoryPage, viewModel.SelectedPage);
        Assert.StartsWith("Undo back to before “Turned on Hair Pack”?", viewModel.ModsTab.Changes.PendingRollback, StringComparison.Ordinal);
        Assert.Single(_scenario.DeployedHair()); // Nothing is undone until it is confirmed there.
    }

    [AvaloniaFact]
    public async Task AProfileNotOnTheGamesDocumentsFolder_IsWorthALookFirst_AndOpensGameFolders()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var elsewhere = MockUnixSupport.Path(@"D:\BioWare\Dragon Age");
        await PointProfileAtAsync(viewModel, elsewhere);

        await viewModel.Home.CheckGameCommand.ExecuteAsync(null);

        var step = viewModel.NextSteps.Steps[0];
        Assert.Equal(NextStepKind.ChooseGameDocumentsFolder, step.Kind);
        Assert.Contains(_scenario.DocumentsDirectory, step.Detail, StringComparison.Ordinal);
        Assert.Contains(elsewhere, step.Detail, StringComparison.Ordinal);
        Assert.True(viewModel.Home.IsReady); // A warning: the game still starts, it just doesn't see the mods.
        ModsTabScenario.Show<NextStepsPanel>(window);
        Screenshots.Save(window, "47-home-documents-not-the-games");

        await viewModel.NextSteps.TakeCommand.ExecuteAsync(step);

        Assert.Equal(MainWindowViewModel.SettingsPage, viewModel.SelectedPage);
        Assert.True(viewModel.GameFoldersOpen);
        Assert.True(_host.FileSystem.Directory.Exists(_scenario.DocumentsDirectory)); // Nothing moved.
    }

    [AvaloniaFact]
    public async Task AProfileOnTheGamesDocumentsFolder_HasNoSuchStep()
    {
        var (_, viewModel) = await _scenario.OpenAsync();

        await viewModel.Home.CheckGameCommand.ExecuteAsync(null);

        Assert.DoesNotContain(viewModel.NextSteps.Steps, s => s.Kind == NextStepKind.ChooseGameDocumentsFolder);
    }

    private async Task PointProfileAtAsync(MainWindowViewModel viewModel, string documents)
    {
        _host.FileSystem.AddDirectory(documents);
        var profiles = _host.Resolve<IProfileRepository>();
        var profile = await profiles.LoadAsync("Default", TestContext.Current.CancellationToken);
        await profiles.SaveAsync(profile! with { UserDocumentsDirectory = documents }, TestContext.Current.CancellationToken);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);
    }
}
