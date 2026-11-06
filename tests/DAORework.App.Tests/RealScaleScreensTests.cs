using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The main screens with nearly every mod of <see cref="RealScaleLibrary"/> on.</summary>
public sealed class RealScaleScreensTests : IDisposable
{
    private const int MaxRows = 40;

    private readonly AppTestHost _host = new();
    private readonly RealScaleScenario _scenario;

    public RealScaleScreensTests()
    {
        _scenario = new RealScaleScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ToBottom_WithNearlyEveryModOn_BringsThePreviewAndApplyIntoView()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        var activation = viewModel.ModsTab.Activation;
        await activation.LeaveOutCommand.ExecuteAsync(null);
        await activation.ApplyCommand.ExecuteAsync(null);
        ModsTabScenario.Show<LoadOrderPanel>(window);
        window.UpdateLayout();
        var order = viewModel.ModsTab.LoadOrder;

        await order.MoveToBottomCommand.ExecuteAsync(order.Order[0]);
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var apply = window.GetLogicalDescendants().OfType<Button>().First(b => b.Name == "ApplyOrderButton");
        var top = apply.TranslatePoint(default, window)!.Value.Y;
        Assert.True(order.IsReordered);
        Assert.True(apply.IsEffectivelyVisible);
        Assert.InRange(top, 0, window.Bounds.Height - apply.Bounds.Height);
        Screenshots.Save(window, "real-scale-screen-arrange-moved");
    }

    [AvaloniaFact]
    public async Task ModRows_AreOneLine_UntilExpanded()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        ModsTabScenario.Show<LibraryPanel>(window);
        window.UpdateLayout();
        var list = window.GetLogicalDescendants().OfType<ItemsControl>().First(i => i.Name == "LibraryMods");

        var closed = list.ContainerFromIndex(0)!.Bounds.Height;
        viewModel.Library.Mods[0].IsExpanded = true;
        window.UpdateLayout();
        Screenshots.Save(window, "real-scale-rows");

        Assert.InRange(closed, 1, 46);
        Assert.True(list.ContainerFromIndex(0)!.Bounds.Height > closed);
    }

    [AvaloniaFact]
    public async Task EveryMainScreen_WithNearlyEveryModOn_StaysReadable()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        var activation = viewModel.ModsTab.Activation;
        await activation.LeaveOutCommand.ExecuteAsync(null);
        await activation.ApplyCommand.ExecuteAsync(null);
        Assert.False(activation.IsError, activation.StatusMessage);

        var order = viewModel.ModsTab.LoadOrder;
        Assert.True(order.Conflicts.Count > MaxRows);
        Assert.InRange(order.SharedRows.Count, 1, MaxRows);
        Assert.Contains(order.Order, r => r.Name == "Rules core");
        Assert.StartsWith("On now: 66 mods", viewModel.ModsTab.Changes.Summary, StringComparison.Ordinal);

        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "real-scale-screen-mods");
        ModsTabScenario.Show<CombinedFilesPanel>(window);
        Screenshots.Save(window, "real-scale-screen-combined");
        ModsTabScenario.Show<LoadOrderPanel>(window);
        Screenshots.Save(window, "real-scale-screen-arrange");
        ModsTabScenario.Show<ChangesPanel>(window);
        Screenshots.Save(window, "real-scale-screen-history");
    }
}
