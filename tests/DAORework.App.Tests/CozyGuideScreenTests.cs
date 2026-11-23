using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The cozy guide on Home (P2.9-03, ADR-0051): where it starts, where its buttons go, and closing it for good.</summary>
public sealed class CozyGuideScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public CozyGuideScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ANewPlayer_StartsAtFindYourGame_InPlaceOfHomesCard()
    {
        var window = _host.Resolve<MainWindowViewModel>();
        await window.InitializeAsync(TestContext.Current.CancellationToken);
        var guide = window.Home.Guide;

        Assert.True(guide.IsShown);
        Assert.Equal(GuideStepKind.FindYourGame, guide.Current!.Kind);
        Assert.Equal("Game folders", guide.Current.Button);

        await guide.TakeStepCommand.ExecuteAsync(null);

        Assert.Equal(MainWindowViewModel.SettingsPage, window.SelectedPage);
        Assert.True(window.GameFoldersOpen);
    }

    [AvaloniaFact]
    public async Task APlayerWhoAddedModsFirst_StartsAtTurnThemOn_ThenPlayFinishesIt()
    {
        _host.SeedRealisticInstall();
        _host.FileSystem.Directory.Delete(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), recursive: true); // Nothing to take over.
        var (window, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        await _scenario.ImportHairPackAsync(viewModel);
        var guide = viewModel.Home.Guide;

        Assert.Equal(GuideStepKind.TurnThemOn, guide.Current!.Kind);
        Assert.Equal([true, true, false, false], guide.Steps.Select(s => s.IsDone));
        viewModel.SelectedPage = MainWindowViewModel.HomePage;
        window.UpdateLayout();
        Screenshots.Save(window, "cozy-guide");

        await guide.TakeStepCommand.ExecuteAsync(null);
        Assert.Equal(MainWindowViewModel.ModsPage, viewModel.SelectedPage);
        await _scenario.ActivateHairPackAsync(viewModel);

        Assert.True(guide.IsShown); // On to Play, though a mod is on now.
        Assert.Equal(GuideStepKind.Play, guide.Current!.Kind);

        await guide.TakeStepCommand.ExecuteAsync(null);

        Assert.False(guide.IsShown); // The game started, so the guide is finished.
        Assert.True(guide.JustFinished);
        Assert.True((await _host.Resolve<IPreferencesStore>().LoadAsync(TestContext.Current.CancellationToken)).Value.GuideClosed);
    }

    [AvaloniaFact]
    public async Task APlayerWithModsOnAlready_NeverSeesIt()
    {
        var (_, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);

        var reopened = _host.Resolve<MainWindowViewModel>();
        await reopened.InitializeAsync(TestContext.Current.CancellationToken);
        await reopened.SelectProfileAsync("Default", TestContext.Current.CancellationToken);

        Assert.False(reopened.Home.Guide.IsShown);
    }

    [AvaloniaFact]
    public async Task Skipping_ClosesItForGood_AndSettingsBringsItBackAtTheFirstStepNotDone()
    {
        var (_, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        var guide = viewModel.Home.Guide;

        await guide.SkipCommand.ExecuteAsync(null);

        Assert.False(guide.IsShown);
        Assert.False(guide.JustFinished); // Skipping isn't finishing, so You're set isn't said.
        var reopened = _host.Resolve<MainWindowViewModel>(); // The window opened again, with where the player is known.
        await reopened.InitializeAsync(TestContext.Current.CancellationToken);
        await reopened.SelectProfileAsync("Default", TestContext.Current.CancellationToken);
        Assert.False(reopened.Home.Guide.IsShown);
        Assert.False(reopened.Home.ShowsGuide);

        await guide.ShowAgainCommand.ExecuteAsync(null);

        Assert.True(guide.IsShown);
        Assert.Equal(GuideStepKind.AddMods, guide.Current!.Kind);
    }
}
