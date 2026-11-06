using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DAORework.Core.Mods.Apply;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A change that takes minutes says how far it has got, and nothing else can start meanwhile (P2.6-22).</summary>
public sealed class ChangeInProgressScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public ChangeInProgressScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task WhileAChangeRuns_TheWindowSaysHowFarItHasGot_AndLocks_ThenComesBack()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var finish = new TaskCompletionSource<int>();
        IProgress<ChangeSteps>? progress = null;

        var running = viewModel.Change.RunAsync("Turn on 77 mods", p =>
        {
            progress = p;
            return finish.Task;
        });
        window.UpdateLayout();

        var banner = window.FindControl<Border>("ChangeBanner")!;
        var pages = window.FindControl<TabControl>("Pages")!;
        Assert.True(banner.IsVisible);
        Assert.False(pages.IsEnabled);
        Assert.False(viewModel.Change.HasSteps);

        progress!.Report(new ChangeSteps(2300, 5675));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal("2300 of 5675 done. Leave DAORework open until this finishes.", viewModel.Change.Detail);
        Assert.InRange(viewModel.Change.Percent, 40, 41);
        Screenshots.Save(window, "change-in-progress");

        finish.SetResult(1);
        await running;
        window.UpdateLayout();

        Assert.False(banner.IsVisible);
        Assert.True(pages.IsEnabled);
    }

    [AvaloniaFact]
    public async Task TurningOnAMod_RunsAsTheChangeInProgress_NamedAsItsPreviewWas()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var titles = new List<string?>();
        viewModel.Change.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "IsRunning" && viewModel.Change.IsRunning)
            {
                titles.Add(viewModel.Change.Title);
            }
        };

        await _scenario.ActivateHairPackAsync(viewModel);

        Assert.Equal("Turn on Hair Pack", Assert.Single(titles));
        Assert.False(viewModel.Change.IsRunning);
    }
}
