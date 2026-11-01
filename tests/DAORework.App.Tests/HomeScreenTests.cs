using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
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
        Assert.Equal("Everything is in place", home.Setup.Title);
        Assert.Equal("Turned on Hair Pack", home.LastChange);
        viewModel.SelectedPage = MainWindowViewModel.HomePage;
        window.UpdateLayout();
        Screenshots.Save(window, "31-home");

        await home.UndoLastCommand.ExecuteAsync(null);

        Assert.Equal(MainWindowViewModel.ModsPage, viewModel.SelectedPage);
        Assert.Equal(MainWindowViewModel.HistorySection, viewModel.ModsSection);
        Assert.StartsWith("Undo back to before “Turned on Hair Pack”?", viewModel.ModsTab.Changes.PendingRollback, StringComparison.Ordinal);
        Assert.Single(_scenario.DeployedHair()); // Nothing is undone until it is confirmed there.
    }
}
