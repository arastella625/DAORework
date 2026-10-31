using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The Mods tab's <i>Load order</i> (P2.5-05): which mod's copy of a shared file wins, and reordering through a plan.</summary>
public sealed class LoadOrderScreenTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public LoadOrderScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task SharedFile_LaterModWins_AndMovingItUpShowsThePlanThenApplies()
    {
        var (window, viewModel) = await OpenWithTwoHairModsAsync();
        var order = viewModel.ModsTab.LoadOrder;

        Assert.Equal(["1. Red Hair", "2. Blue Hair"], order.Order.Select(r => r.Label));
        var conflict = Assert.Single(order.Conflicts);
        Assert.Equal("hair.dds", conflict.FileName);
        Assert.Equal("Blue Hair wins", conflict.Winner);
        Assert.Equal("Also in Red Hair, whose copy is not used.", conflict.Detail);
        Assert.Equal("blue", _scenario.DeployedContent("hair.dds"));

        await order.MoveUpCommand.ExecuteAsync(order.Order[1]);

        Assert.True(order.IsReordered);
        Assert.Equal(["1. Blue Hair", "2. Red Hair"], order.Order.Select(r => r.Label));
        Assert.Equal("Red Hair wins", Assert.Single(order.Conflicts).Winner);
        Assert.Contains("  Copy      1 file", order.PlanLines);
        Assert.Contains("  Delete    1 file", order.PlanLines);
        Assert.True(order.ApplyCommand.CanExecute(null));
        Assert.Equal("blue", _scenario.DeployedContent("hair.dds"));
        ModsTabScenario.Show<LoadOrderPanel>(window);
        Screenshots.Save(window, "17-load-order");

        await order.ApplyCommand.ExecuteAsync(null);

        Assert.False(order.IsError, order.StatusMessage);
        Assert.StartsWith("Done: the new order is applied", order.StatusMessage, StringComparison.Ordinal);
        Assert.False(order.IsReordered);
        Assert.Equal(["1. Blue Hair", "2. Red Hair"], order.Order.Select(r => r.Label));
        Assert.Equal("red", _scenario.DeployedContent("hair.dds"));
        Assert.Equal(3, viewModel.ModsTab.Changes.Backups.Count);
    }

    [AvaloniaFact]
    public async Task Reset_GoesBackToTheSavedOrderAndChangesNothing()
    {
        var (_, viewModel) = await OpenWithTwoHairModsAsync();
        var order = viewModel.ModsTab.LoadOrder;
        await order.MoveDownCommand.ExecuteAsync(order.Order[0]);

        await order.ResetCommand.ExecuteAsync(null);

        Assert.Equal("Back to the saved order; nothing was changed.", order.StatusMessage);
        Assert.False(order.IsReordered);
        Assert.Empty(order.PlanLines);
        Assert.False(order.ApplyCommand.CanExecute(null));
        Assert.Equal(["1. Red Hair", "2. Blue Hair"], order.Order.Select(r => r.Label));
        Assert.Equal("blue", _scenario.DeployedContent("hair.dds"));
    }

    [AvaloniaFact]
    public async Task NewOrder_ThatChangesNoFile_SaysTheOrderMakesNoDifference()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Eyes.zip"), ("eyes.dds", "eyes"));
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Nails.zip"), ("nails.dds", "nails"));
        await _scenario.ActivateAsync(viewModel, "Eyes");
        await _scenario.ActivateAsync(viewModel, "Nails");
        var order = viewModel.ModsTab.LoadOrder;
        Assert.Empty(order.Conflicts);

        await order.MoveUpCommand.ExecuteAsync(order.Order[1]);

        Assert.True(order.IsReordered);
        Assert.Contains("No file changes: none of these mods ships a file another one does, so their order makes no difference.", order.PlanLines);
        Assert.False(order.ApplyCommand.CanExecute(null));
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenWithTwoHairModsAsync()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Red Hair.zip"), ("hair.dds", "red"), ("red_extra.dds", "x"));
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Blue Hair.zip"), ("hair.dds", "blue"));
        await _scenario.ActivateAsync(viewModel, "Red Hair");
        await _scenario.ActivateAsync(viewModel, "Blue Hair");
        return (window, viewModel);
    }
}
