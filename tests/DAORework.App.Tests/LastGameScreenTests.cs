using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>What changed since the last game with other mods (ADR-0043).</summary>
public sealed class LastGameScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public LastGameScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task AfterAddingMods_SaysWhatsNew_AndCanGoBack()
    {
        var (_, viewModel) = await OpenAsync();
        var tab = viewModel.ModsTab;
        await TurnOnAsync(viewModel, "First");
        await viewModel.Launch.LaunchCommand.ExecuteAsync(null);
        Assert.Null(tab.LastGame.Headline);
        var before = tab.Activation.ActiveModIds.ToList();

        await TurnOnAsync(viewModel, "Second", "Third");

        Assert.StartsWith("If your game last worked ", tab.LastGame.Headline, StringComparison.Ordinal);
        Assert.Equal("Added 2: Second and Third.", Assert.Single(tab.LastGame.Lines));

        await tab.LastGame.GoBackCommand.ExecuteAsync(null);
        await tab.LastGame.ApplyCommand.ExecuteAsync(null);

        Assert.False(tab.LastGame.IsError, tab.LastGame.StatusMessage);
        Assert.Equal(before, tab.Activation.ActiveModIds);
        Assert.Null(tab.LastGame.Headline);
    }

    [AvaloniaFact]
    public async Task PlayingTheBrokenSet_StillComparesWithTheGameBefore()
    {
        var (_, viewModel) = await OpenAsync();
        var tab = viewModel.ModsTab;
        await TurnOnAsync(viewModel, "First");
        await viewModel.Launch.LaunchCommand.ExecuteAsync(null);
        await TurnOnAsync(viewModel, "Second");

        await viewModel.Launch.LaunchCommand.ExecuteAsync(null);

        Assert.Equal("Added 1: Second.", Assert.Single(tab.LastGame.Lines));
    }

    [AvaloniaFact]
    public async Task AGameDuringASearch_IsNotRecorded()
    {
        var (_, viewModel) = await OpenAsync();
        var tab = viewModel.ModsTab;
        await TurnOnAsync(viewModel, "First", "Second", "Third");
        await viewModel.Launch.LaunchCommand.ExecuteAsync(null);
        await tab.ProblemSearch.StartCommand.ExecuteAsync(null);
        await tab.ProblemSearch.ApplyCommand.ExecuteAsync(null);

        await viewModel.Launch.LaunchCommand.ExecuteAsync(null);
        await tab.ProblemSearch.PutBackCommand.ExecuteAsync(null);
        await tab.ProblemSearch.ApplyCommand.ExecuteAsync(null);

        Assert.Null(tab.LastGame.Headline); // Back to the set of the only game that counted.
    }

    [AvaloniaFact]
    public async Task GoingBack_AModNoLongerInTheLibrary_IsLeftOut_AndSaid()
    {
        var (_, viewModel) = await OpenAsync();
        var tab = viewModel.ModsTab;
        await TurnOnAsync(viewModel, "First", "Second");
        await viewModel.Launch.LaunchCommand.ExecuteAsync(null);
        await tab.Activation.PlanChangeAsync(ModChangeKind.Deactivate, [Row(viewModel, "Second")], TestContext.Current.CancellationToken);
        await tab.Activation.ApplyCommand.ExecuteAsync(null);
        var second = Row(viewModel, "Second").Id;
        tab.LastGame.InLibrary = id => id != second;

        await tab.LastGame.GoBackCommand.ExecuteAsync(null);

        Assert.StartsWith("1 mod from then isn't in your Library any more", tab.LastGame.Preview.Notes[0], StringComparison.Ordinal);
    }

    private static LibraryModRow Row(MainWindowViewModel viewModel, string title) => viewModel.Library.Mods.Single(m => m.Title == title);

    private static async Task TurnOnAsync(MainWindowViewModel viewModel, params string[] titles)
    {
        var activation = viewModel.ModsTab.Activation;
        await activation.PlanChangeAsync(ModChangeKind.Activate, [.. titles.Select(t => Row(viewModel, t))], TestContext.Current.CancellationToken);
        await activation.ApplyCommand.ExecuteAsync(null);
        Assert.False(activation.IsError, activation.StatusMessage);
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync()
    {
        var opened = await _scenario.OpenAsync();
        var paths = opened.ViewModel.Profile.Paths;
        _host.Picker.Enqueue(AppTestHost.InstallDirectory);
        await paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Install);
        _host.Picker.Enqueue(GamePaths.GameExecutable(AppTestHost.InstallDirectory));
        await paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Game);
        _host.Picker.Enqueue(GamePaths.LauncherExecutable(AppTestHost.InstallDirectory));
        await paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Launcher);
        foreach (var name in new[] { "First", "Second", "Third" })
        {
            await _scenario.ImportAsync(opened.ViewModel, MockUnixSupport.Path($@"C:\Users\Me\Downloads\{name}.zip"), ($"{name}.dds", name));
        }

        return opened;
    }
}
