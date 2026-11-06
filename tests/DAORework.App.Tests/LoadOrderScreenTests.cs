using System.IO.Abstractions.TestingHelpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
        Assert.Equal("Also in Red Hair, whose copy is not used. It is a texture or model, so only that mod's look shows.", conflict.Detail);
        Assert.Equal("blue", _scenario.DeployedContent("hair.dds"));

        await order.MoveUpCommand.ExecuteAsync(order.Order[1]);

        Assert.True(order.IsReordered);
        Assert.Equal(["1. Blue Hair", "2. Red Hair"], order.Order.Select(r => r.Label));
        Assert.Equal("Red Hair wins", Assert.Single(order.Conflicts).Winner);
        Assert.Contains("  Copy      1 file", order.PlanLines);
        Assert.Contains("  Delete    1 file", order.PlanLines);
        Assert.Equal("Copies 1 file and deletes 1. Writes 3 B.", order.Preview.Summary);
        Assert.Equal("hair.dds is in Blue Hair and Red Hair; Red Hair's copy is used. It is a texture or model, so only that mod's look shows.", Assert.Single(order.Preview.Notes));
        Assert.True(order.ApplyCommand.CanExecute(null));
        Assert.Equal("blue", _scenario.DeployedContent("hair.dds"));
        ModsTabScenario.Show<LoadOrderPanel>(window);
        Screenshots.Save(window, "17-load-order");

        await order.ApplyCommand.ExecuteAsync(null);

        Assert.False(order.IsError, order.StatusMessage);
        Assert.StartsWith("Saved the new order", order.StatusMessage, StringComparison.Ordinal);
        Assert.False(order.IsReordered);
        Assert.Equal(["1. Blue Hair", "2. Red Hair"], order.Order.Select(r => r.Label));
        Assert.Equal("red", _scenario.DeployedContent("hair.dds"));
        Assert.Equal(3, viewModel.ModsTab.Changes.Backups.Count);
    }

    [AvaloniaFact]
    public async Task Rows_SayWhichModWinsAFileTheyShare_AndFollowTheSavedOrder()
    {
        var (_, viewModel) = await OpenWithTwoHairModsAsync();
        var mods = viewModel.Library.Mods;

        Assert.Equal("wins", mods.Single(m => m.Title == "Blue Hair").ConflictMark);
        Assert.Equal("loses", mods.Single(m => m.Title == "Red Hair").ConflictMark);

        var order = viewModel.ModsTab.LoadOrder;
        await order.MoveUpCommand.ExecuteAsync(order.Order[1]);

        Assert.Equal("wins", mods.Single(m => m.Title == "Blue Hair").ConflictMark);

        await order.ApplyCommand.ExecuteAsync(null);

        Assert.Equal("wins", viewModel.Library.Mods.Single(m => m.Title == "Red Hair").ConflictMark);
    }

    [AvaloniaFact]
    public async Task DraggingAModByItsGrip_MovesItToWhereItIsDropped_WithOnePreview()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        foreach (var name in new[] { "Eyes", "Lips", "Nails", "Teeth" })
        {
            await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, $"{name}.zip"), ($"{name}.dds", name));
            await _scenario.ActivateAsync(viewModel, name);
        }

        var order = viewModel.ModsTab.LoadOrder;
        ModsTabScenario.Show<LoadOrderPanel>(window);
        window.UpdateLayout();
        var grips = window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("dragGrip")).ToList();
        var start = grips[0].TranslatePoint(new Point(4, 4), window)!.Value;
        var end = grips[2].TranslatePoint(new Point(4, grips[2].Bounds.Height), window)!.Value + new Point(0, 6);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(end);
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.GetLogicalDescendants().OfType<Border>().First(b => b.Name == "DropLine").IsVisible);
        window.MouseUp(end, MouseButton.Left);
        for (var i = 0; i < 100 && !order.IsReordered; i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
        }

        Assert.Equal(["1. Lips", "2. Nails", "3. Eyes", "4. Teeth"], order.Order.Select(r => r.Label));
        Assert.True(order.IsReordered);
        Assert.False(window.GetLogicalDescendants().OfType<Border>().First(b => b.Name == "DropLine").IsVisible);
        Screenshots.Save(window, "arrange-dragged");
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 3, 2)]
    [InlineData(2, 0, 0)]
    [InlineData(2, 2, 2)]
    [InlineData(2, 4, 3)]
    public void Place_TakesTheModItselfOutOfTheCount(int from, int gap, int expected)
    {
        Assert.Equal(expected, ArrangeDrop.Place(from, gap));
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
    public async Task ToTopAndToBottom_MoveAModPastTheOthersInOneStep_AndKeepTheirOrder()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        foreach (var name in new[] { "Eyes", "Lips", "Nails", "Teeth" })
        {
            await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, $"{name}.zip"), ($"{name}.dds", name));
            await _scenario.ActivateAsync(viewModel, name);
        }

        var order = viewModel.ModsTab.LoadOrder;

        await order.MoveToBottomCommand.ExecuteAsync(order.Order[0]);

        Assert.Equal(["1. Lips", "2. Nails", "3. Teeth", "4. Eyes"], order.Order.Select(r => r.Label));
        Assert.True(order.IsReordered);

        await order.MoveToTopCommand.ExecuteAsync(order.Order[3]);

        Assert.Equal(["1. Eyes", "2. Lips", "3. Nails", "4. Teeth"], order.Order.Select(r => r.Label));
        Assert.False(order.IsReordered);

        await order.MoveToTopCommand.ExecuteAsync(order.Order[0]);
        await order.MoveToBottomCommand.ExecuteAsync(order.Order[3]);

        Assert.False(order.IsReordered);
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
        Assert.Equal("No file changes: none of these mods ships a file another one does, so their order makes no difference.", order.Preview.Summary);
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
