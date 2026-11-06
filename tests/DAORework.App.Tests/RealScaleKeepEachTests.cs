using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary><i>Keep one of each</i> on a library the size of the owner's: 139 entries, 51 sets of copies and about 6 GB of extras.</summary>
public sealed class RealScaleKeepEachTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly RealScaleScenario _scenario;

    public RealScaleKeepEachTests()
    {
        _scenario = new RealScaleScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaTheory]
    [InlineData(960, 640, "real-scale-04-keep-one-of-each")]
    [InlineData(720, 480, "real-scale-05-keep-one-of-each-smallest")]
    public async Task FiftyOneSets_FitOneScreen_WithWhatDiffersFirst(int width, int height, string screenshot)
    {
        var (window, viewModel) = await _scenario.OpenAsync(withCopies: true);
        window.Width = width;
        window.Height = height;
        Settle(window);
        var opener = Find<Button>(window, "KeepEachButton");
        Assert.InRange(opener.TranslatePoint(default, window)!.Value.X + opener.Bounds.Width, 0, width);
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);
        Settle(window);

        Assert.Equal(51, keep.Rows.Count);
        Assert.Equal("Removes 63 copies of 48 mods from your Library, and gets back 5.34 GB.", keep.Preview.Summary);
        Assert.Equal("Remove 63 copies", keep.RemoveLabel);
        Assert.Contains("3 mods have copies that aren't the same, so they come first and stay unless you pick one.", keep.Preview.Notes);
        Assert.All(keep.Rows.Take(3), r => Assert.Null(r.Pick!.ModId));
        Assert.All(keep.Rows.Take(3), r => Assert.StartsWith("These copies aren't the same", r.Note, StringComparison.Ordinal));
        Assert.Equal(2, keep.Rows.Skip(3).Count(r => r.Note is not null));
        Assert.Equal(48, viewModel.Library.CopiesToClear);
        Assert.True(keep.CanRemove);

        var sheet = Find<Border>(window, "KeepEachSheet");
        var top = sheet.TranslatePoint(default, window)!.Value.Y;
        Assert.InRange(top, 0, height);
        Assert.InRange(top + sheet.Bounds.Height, 0, height);

        var button = Find<Button>(window, "RemoveCopiesButton");
        var buttonTop = button.TranslatePoint(default, window)!.Value.Y;
        Assert.InRange(buttonTop + button.Bounds.Height, 0, height);

        var scroller = Find<ScrollViewer>(window, "KeepEachScroller");
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height);
        Assert.True(scroller.Viewport.Height >= 80, $"Only {scroller.Viewport.Height} of the list shows.");
        Screenshots.Save(window, screenshot);
    }

    [AvaloniaFact]
    public async Task RemovingThemAll_LeavesOneOfEach_AndTheGameAsItWas()
    {
        var (_, viewModel) = await _scenario.OpenAsync(withCopies: true);
        var active = viewModel.ModsTab.Activation.ActiveModIds.ToList();
        var backups = viewModel.ModsTab.Changes.Backups.Count;
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);

        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Equal(RealScaleLibrary.Entries.Count - 1 + 6, viewModel.Library.Mods.Count); // The add-ins' copies differ, so they stay.
        Assert.Equal(3, viewModel.Library.CopySets);
        Assert.Equal(0, viewModel.Library.CopiesToClear);
        Assert.Equal(active, viewModel.ModsTab.Activation.ActiveModIds);
        Assert.Equal(backups, viewModel.ModsTab.Changes.Backups.Count);
    }

    [AvaloniaFact]
    public async Task PickingACopyThatIsOff_AtThisSize_StaysInItsPlace_AndAddsOneSwap()
    {
        var (_, viewModel) = await _scenario.OpenAsync(withCopies: true);
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);
        var order = keep.Rows.Select(r => r.Group).ToList();
        var row = keep.Rows.First(r => r.Pick!.Label.Contains(", on", StringComparison.Ordinal) && r.Note is null);

        row.Pick = row.Choices.First(c => c.ModId is not null && !c.Label.Contains(", on", StringComparison.Ordinal));
        await keep.ReplanCommand.ExecutionTask!;

        Assert.Equal(order, keep.Rows.Select(r => r.Group));
        Assert.Contains("is on, so this one takes its place.", row.Note, StringComparison.Ordinal);
        Assert.True(keep.CanRemove, string.Join(" | ", keep.Preview.Problems.Select(p => p.Title)));
    }

    private static T Find<T>(MainWindow window, string name)
        where T : Control => window.GetLogicalDescendants().OfType<T>().First(c => c.Name == name);

    private static void Settle(MainWindow window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
