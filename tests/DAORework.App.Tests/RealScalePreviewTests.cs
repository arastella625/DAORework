using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Turning on dozens of mods: whether it can go ahead, what stops it and what to do, in one screen.</summary>
public sealed class RealScalePreviewTests : IDisposable
{
    private const int MaxNotes = 10;

    private readonly AppTestHost _host = new();
    private readonly RealScaleScenario _scenario;

    public RealScalePreviewTests()
    {
        _scenario = new RealScaleScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Fixture_IsTheSizeOfARealLibrary()
    {
        var files = RealScaleLibrary.Entries.SelectMany(e => e.Mod.Files).ToList();

        Assert.Equal(70, RealScaleLibrary.Entries.Count);
        Assert.InRange(files.Count, 7000, 10000);
        Assert.InRange(files.Count(f => f.RelativePath.EndsWith("__folder_managed_by_vortex", StringComparison.Ordinal)), 900, 1000);
    }

    [AvaloniaFact]
    public async Task TurnOnSelected_Blocked_PutsWhatStopsItFirst_AndReadsInOneScreen()
    {
        var (window, viewModel) = await _scenario.OpenAsync();

        await RealScaleScenario.TurnOnEverythingAsync(viewModel);

        var activation = viewModel.ModsTab.Activation;
        var preview = activation.Preview;
        Assert.Equal("Turn on 63 mods", preview.Title);
        Assert.StartsWith("Can't go ahead yet: 3 problems", preview.Summary, StringComparison.Ordinal);
        Assert.Contains(preview.Problems, p => p.Title.StartsWith(RealScaleLibrary.BlockedProgram, StringComparison.Ordinal));
        Assert.Contains(preview.Problems, p => p.Title.StartsWith(RealScaleLibrary.BlockedRegistered, StringComparison.Ordinal));
        Assert.Contains(preview.Problems, p => p.Title.Contains(RealScaleLibrary.BlockedInTheWay, StringComparison.Ordinal));
        Assert.All(preview.Problems, p => Assert.NotEmpty(p.WhatToDo));
        Assert.InRange(preview.Notes.Count, 1, MaxNotes);
        Assert.DoesNotContain(preview.Notes, n => n.Contains("__folder_managed_by_vortex", StringComparison.Ordinal) || n.Contains("readme.txt", StringComparison.Ordinal) || n.Contains("add-ins", StringComparison.Ordinal));
        Assert.False(activation.ApplyCommand.CanExecute(null));

        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "real-scale-01-blocked-preview");
    }

    [AvaloniaFact]
    public async Task TurnOn_FromFarDownTheList_BringsTheWholePreviewAndItsButtonsIntoView()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var panel = window.GetLogicalDescendants().OfType<LibraryPanel>().Distinct().Single();
        window.UpdateLayout();
        var scroller = panel.FindAncestorOfType<ScrollViewer>()!;
        scroller.Offset = new Vector(0, scroller.Extent.Height);
        window.UpdateLayout();

        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var border = panel.GetLogicalDescendants().OfType<Border>().First(b => b.Name == "PlanPreview");
        var top = border.TranslatePoint(default, scroller)!.Value.Y;
        Assert.InRange(top, 0, scroller.Viewport.Height);
        Assert.InRange(top + border.Bounds.Height, 0, scroller.Viewport.Height);
        Screenshots.Save(window, "real-scale-03-preview-in-view");
    }

    [AvaloniaFact]
    public async Task LeaveOut_TakesOutWhatStopsIt_ThenTheRestCanBeApplied()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        var activation = viewModel.ModsTab.Activation;
        Assert.Equal("Leave out 3 mods", activation.LeaveOutLabel);

        await activation.LeaveOutCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Equal("Turn on 60 mods", activation.Preview.Title);
        Assert.Empty(activation.Preview.Problems);
        Assert.StartsWith("Left out: ", activation.Preview.Notes[0], StringComparison.Ordinal);
        Assert.True(activation.ApplyCommand.CanExecute(null));
        Assert.False(activation.CanLeaveOut);
        Assert.DoesNotContain(activation.Preview.Notes, n => n.Contains("mean nothing to the game", StringComparison.Ordinal));
        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "real-scale-02-ready-preview");
    }
}
