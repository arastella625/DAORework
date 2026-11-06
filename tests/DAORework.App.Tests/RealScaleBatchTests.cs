using Avalonia.Headless.XUnit;
using DAORework.App.Views;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Mods.Conflicts;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A batch at real size going on without what fails (P2.6-53).</summary>
public sealed class RealScaleBatchTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly RealScaleScenario _scenario;

    public RealScaleBatchTests()
    {
        _scenario = new RealScaleScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TurnOnEverything_OneModFailsPartway_TheOthersComeBack_AndGoAhead()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var activation = viewModel.ModsTab.Activation;
        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        await activation.LeaveOutCommand.ExecuteAsync(null);
        Assert.Equal("Turn on 59 mods", activation.Preview.Title);
        var failing = viewModel.Library.Mods.First(m => !m.IsActive && m.IsSelected && !RealScaleLibrary.Blocked.Contains(m.Title));
        foreach (var file in RealScaleLibrary.Entries.Single(e => e.Mod.Id == failing.Id).Mod.Files)
        {
            _host.FileSystem.RemoveFile(Path.Combine(_host.LibraryDirectory, failing.Id, file.RelativePath));
        }

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Equal("Turn on 58 mods", activation.Preview.Title);
        Assert.False(viewModel.ModsTab.Changes.ChangeUnfinished);
        Assert.StartsWith("Left out: ", activation.Preview.Notes[0], StringComparison.Ordinal);
        Assert.Contains(activation.Preview.Lines, l => l.StartsWith($"Left out {failing.Title}: ", StringComparison.Ordinal));

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.StartsWith("Turned on 58 mods. 4 need a look.", activation.StatusMessage, StringComparison.Ordinal);
        Assert.False(viewModel.ModsTab.Changes.ChangeUnfinished);
    }

    [AvaloniaFact]
    public async Task AModInsideABiggerOne_OfAnotherName_SaysSo_AndOffersToKeepTheBiggerOne()
    {
        var whole = RealScaleLibrary.Entries.First(e => e.Mod.Name == "Big Texture Pack");
        var mod = whole.Mod with { Id = "lite-pack-1", Name = "Lite Pack", Files = [.. whole.Mod.Files.Take(20)] };
        var (_, viewModel) = await _scenario.OpenAsync(extra: [whole with { Mod = mod }]);

        var row = viewModel.Library.Mods.Single(m => m.Title == "Lite Pack");

        Assert.Equal(whole.Mod.Id, row.Inside!.WholeId);
        Assert.Equal("Keep Big Texture Pack", row.KeepWholeLabel);
        Assert.Contains(row.Notes, n => n.StartsWith("Everything in this mod is also in Big Texture Pack", StringComparison.Ordinal));
        Assert.Null(row.Copy);

        await viewModel.ModsTab.KeepCopy.PlanKeepWholeCommand.ExecuteAsync(row);

        var keep = viewModel.ModsTab.KeepCopy;
        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Equal("Keep Big Texture Pack", keep.Preview.Title);
        Assert.Equal("Remove Lite Pack", keep.RemoveLabel);
        Assert.StartsWith("Removes Lite Pack from your Library", keep.Preview.Summary, StringComparison.Ordinal);

        await keep.RemoveOthersCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.DoesNotContain(viewModel.Library.Mods, m => m.Title == "Lite Pack");
    }

    [AvaloniaFact]
    public async Task WhatChanged_AfterTurningOnNearlyEverything_ReadsInAFewLines()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var tab = viewModel.ModsTab;
        await _host.Resolve<RecordPlay>().ExecuteAsync(new ModsTabScenario(_host).DocumentsDirectory, TestContext.Current.CancellationToken);

        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        await tab.Activation.LeaveOutCommand.ExecuteAsync(null);
        await tab.Activation.ApplyCommand.ExecuteAsync(null);

        Assert.StartsWith("If your game last worked ", tab.LastGame.Headline, StringComparison.Ordinal);
        var added = Assert.Single(tab.LastGame.Lines);
        Assert.StartsWith("Added 59: ", added, StringComparison.Ordinal);
        Assert.InRange(added.Length, 1, 120);
    }

    [AvaloniaFact]
    public async Task CombinedFiles_WithNearlyEveryModOn_SaysWhatMatters_MostSeriousFirst()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var activation = viewModel.ModsTab.Activation;
        await RealScaleScenario.TurnOnEverythingAsync(viewModel);
        await activation.LeaveOutCommand.ExecuteAsync(null);
        await activation.ApplyCommand.ExecuteAsync(null);
        var order = viewModel.ModsTab.LoadOrder;

        Assert.Contains("only change how things look", order.SharedSummary, StringComparison.Ordinal);
        var impacts = order.SharedRows.Where(r => !r.SameContent).Select(r => r.Impact).ToList();
        Assert.NotEmpty(impacts);
        Assert.Equal(impacts.Order(), impacts);
        Assert.Equal(SharedFileImpact.CouldBreakQuests, order.Conflicts[0].Impact);
        ModsTabScenario.Show<CombinedFilesPanel>(window);
        Screenshots.Save(window, "real-scale-combined-ranked");
    }
}
