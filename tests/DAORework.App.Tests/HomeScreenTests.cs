using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Home, the first tab (ADR-0026, P2.5-25): one headline and one next step (ADR-0050 §1), and the last change with Undo.</summary>
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
    public async Task NoProfile_SaysWelcome_AndOpensGameFolders()
    {
        var window = _host.Resolve<MainWindowViewModel>();
        var home = window.Home;

        Assert.Equal(MainWindowViewModel.HomePage, window.SelectedPage); // Home opens first.
        Assert.Equal("Welcome", home.Card.Title);
        Assert.Equal("Game folders", home.Card.Button);
        Assert.False(home.HasLastChange);

        await home.TakeCardCommand.ExecuteAsync(null);

        Assert.Equal(MainWindowViewModel.SettingsPage, window.SelectedPage);
        Assert.True(window.GameFoldersOpen);
    }

    [AvaloniaFact]
    public async Task OpeningAProfile_ChecksTheGame_AndSaysTheOneThingFirst()
    {
        var (_, viewModel) = await _scenario.OpenAsync();

        await viewModel.CheckingGame;

        Assert.True(viewModel.Diagnostics.HasReport); // Checked on open, with no button pressed.
        Assert.Equal("One thing first", viewModel.Home.Card.Title);
        Assert.Equal("4 mods are in your game.", viewModel.Home.Card.Sentence); // The seeded install's hand-placed mods.
        Assert.Equal("Take them over", viewModel.Home.Card.Button);
    }

    [AvaloniaFact]
    public async Task TurningOnAMod_ChecksAgain_SaysReady_AndUndoFromHomeAsksInHistory()
    {
        _host.SeedRealisticInstall();
        _host.FileSystem.Directory.Delete(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), recursive: true); // Nothing to take over first.
        var (window, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        await _scenario.ImportHairPackAsync(viewModel);
        await viewModel.CheckingGame;
        var checks = 0;
        var showReport = viewModel.Diagnostics.AfterReportAsync!;
        viewModel.Diagnostics.AfterReportAsync = (profile, issues, token) =>
        {
            checks++;
            return showReport(profile, issues, token);
        };

        await _scenario.ActivateHairPackAsync(viewModel);
        await viewModel.CheckingGame;

        var home = viewModel.Home;
        Assert.Equal("Ready to play", home.Card.Title);
        Assert.Equal("Play", home.Card.Button);
        Assert.True(home.IsReady);
        Assert.True(checks > 0); // The change was checked, with no button pressed.
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

        await viewModel.CheckingGame;

        var step = viewModel.NextSteps.Steps[0];
        Assert.Equal(NextStepKind.ChooseGameDocumentsFolder, step.Kind);
        Assert.Contains(_scenario.DocumentsDirectory, step.Detail, StringComparison.Ordinal);
        Assert.Contains(elsewhere, step.Detail, StringComparison.Ordinal);
        Assert.False(viewModel.Home.HasProblems); // A warning: the game still starts, it just doesn't see the mods...
        Assert.NotEqual("Ready to play", viewModel.Home.Card.Title); // ...so nothing says all is well with them.
        Assert.True(viewModel.Home.Setup!.GameReadsElsewhere);
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

        await viewModel.CheckingGame;

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
