using Avalonia.Headless.XUnit;
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
