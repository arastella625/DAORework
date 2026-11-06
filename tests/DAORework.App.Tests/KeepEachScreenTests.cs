using System.IO.Abstractions.TestingHelpers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using DAORework.App.ViewModels;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.App.Tests;

/// <summary><i>Keep one of each</i> clears every set of copies from one preview (ADR-0039).</summary>
public sealed class KeepEachScreenTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public KeepEachScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TheButton_ShowsOnlyWhileThereAreCopies()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var button = window.GetLogicalDescendants().OfType<Button>().First(b => b.Name == "KeepEachButton");
        Assert.Equal(0, viewModel.Library.CopySets);
        Assert.False(button.IsVisible);

        await ImportAsync(viewModel, "Hair Pack Copy.zip", "hair_pack.dds", "DDS hair");

        Assert.Equal(1, viewModel.Library.CopySets);
        Assert.True(button.IsVisible);
    }

    [AvaloniaFact]
    public async Task OnePreview_ClearsEverySet_AndChangesNothingInTheGame()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        await _scenario.ActivateAsync(viewModel, "Eyes Copy");
        var backups = viewModel.ModsTab.Changes.Backups.Count;
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);

        Assert.Equal("Keep one of each", keep.Preview.Title);
        Assert.StartsWith("Removes 2 copies of 2 mods from your Library", keep.Preview.Summary, StringComparison.Ordinal);
        Assert.Equal("Remove 2 copies", keep.RemoveLabel);
        Assert.Contains(keep.Preview.Notes, n => n.StartsWith("Removing can't be undone.", StringComparison.Ordinal));
        Assert.Equal(2, keep.Preview.Lines.Count);
        Assert.Equal("Eyes Copy.zip, on, newest", Set(keep, "Eyes").Pick!.Label);
        Assert.Equal("Hair Pack", Set(keep, "Hair Pack").Choices[1].Label);

        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.StartsWith("Kept one copy of 2 mods", keep.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(["Eyes Copy", "Hair Pack Copy"], viewModel.Library.Mods.Select(m => m.Title).Order());
        Assert.Equal(0, viewModel.Library.CopySets);
        Assert.Equal(backups, viewModel.ModsTab.Changes.Backups.Count);
        Assert.True(viewModel.Library.Mods.Single(m => m.Title == "Eyes Copy").IsActive);
        Assert.Null(keep.Preview.Title);
        Assert.Empty(keep.Rows);
    }

    [AvaloniaFact]
    public async Task ChangingPicks_MakesEverySwapInOneChange()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        await _scenario.ActivateAsync(viewModel, "Eyes Copy");
        await _scenario.ActivateAsync(viewModel, "Hair Pack Copy");
        var backups = viewModel.ModsTab.Changes.Backups.Count;
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);

        await PickOtherCopyAsync(keep, "Eyes");
        await PickOtherCopyAsync(keep, "Hair Pack");

        Assert.Contains("is on, so this one takes its place.", Set(keep, "Eyes").Note, StringComparison.Ordinal);
        Assert.True(keep.CanRemove);

        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Equal(["Eyes", "Hair Pack"], viewModel.Library.Mods.Select(m => m.Title).Order());
        Assert.Equal(backups + 1, viewModel.ModsTab.Changes.Backups.Count);
        var active = viewModel.ModsTab.Activation.ActiveModIds;
        Assert.Equal(2, active.Count);
        Assert.StartsWith("eyes-", active[0], StringComparison.Ordinal);
        Assert.StartsWith("hair-pack-", active[1], StringComparison.Ordinal);
        Assert.DoesNotContain(active, id => id.Contains("copy", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task KeepAllOfThem_LeavesThatSet_AndNothingLeftMeansNoButton()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);
        var order = keep.Rows.Select(r => r.Title).ToList();

        await KeepAllAsync(keep, "Eyes");

        Assert.StartsWith("Removes 1 copy of 1 mod", keep.Preview.Summary, StringComparison.Ordinal);
        Assert.Equal("Remove 1 copy", keep.RemoveLabel);
        Assert.Equal(order, keep.Rows.Select(r => r.Title));

        await KeepAllAsync(keep, "Hair Pack");

        Assert.Equal("Nothing is removed, since every copy stays.", keep.Preview.Summary);
        Assert.False(keep.CanRemove);
        Assert.DoesNotContain(keep.Preview.Notes, n => n.StartsWith("Removing can't be undone.", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task CopiesThatArentTheSame_ComeFirst_AndSayWhatWouldGo()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        await ImportAddInAsync(viewModel, "Zeta One.dazip", "version one");
        await ImportAddInAsync(viewModel, "Zeta Two.dazip", "version two");
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);

        Assert.Equal(3, keep.Rows.Count);
        Assert.Contains("Zeta", keep.Rows[0].Title, StringComparison.Ordinal);
        Assert.StartsWith("These copies aren't the same.", keep.Rows[0].Note, StringComparison.Ordinal);
        Assert.Null(keep.Rows[1].Note);
        Assert.Contains("1 mod has copies that aren't the same, so it comes first.", keep.Preview.Notes);
    }

    [AvaloniaFact]
    public async Task ASetOnInTwoProfiles_StaysAsItIs_AndTheOthersGo()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        await _scenario.ActivateAsync(viewModel, "Hair Pack");
        await SelectSecondProfileAsync(viewModel);
        await _scenario.ActivateAsync(viewModel, "Hair Pack Copy");
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);

        Assert.Contains("Hair Pack is on in", Set(keep, "Hair Pack").Stopped, StringComparison.Ordinal);
        Assert.Contains("Turn it off there first.", Set(keep, "Hair Pack").Stopped, StringComparison.Ordinal);
        Assert.Contains("1 mod stays as it is, and says why.", keep.Preview.Notes);
        Assert.Equal("Remove 1 copy", keep.RemoveLabel);
        Assert.True(keep.CanRemove);

        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Equal(3, viewModel.Library.Mods.Count);
        Assert.Equal(1, viewModel.Library.CopySets);
    }

    [AvaloniaFact]
    public async Task ACopyTurnedOnAfterThePreview_StopsIt_AndNothingIsRemoved()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);

        await _scenario.ActivateAsync(viewModel, "Eyes");
        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.True(keep.IsError);
        Assert.Contains(DiagnosticCodes.ModCopyStillOn, keep.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(4, viewModel.Library.Mods.Count);
    }

    [AvaloniaFact]
    public async Task OpeningEitherPreview_ClosesTheOther()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        var each = viewModel.ModsTab.KeepEach;
        var one = viewModel.ModsTab.KeepCopy;

        await one.PlanKeepCommand.ExecuteAsync(viewModel.Library.Mods.First(m => m.Title == "Eyes"));
        await each.PlanCommand.ExecuteAsync(null);

        Assert.Null(one.Preview.Title);
        Assert.False(one.CanRemove);
        Assert.NotNull(each.Preview.Title);

        await one.PlanKeepCommand.ExecuteAsync(viewModel.Library.Mods.First(m => m.Title == "Eyes"));

        Assert.NotNull(one.Preview.Title);
        Assert.Null(each.Preview.Title);
        Assert.False(each.CanRemove);
        Assert.Empty(each.Rows);
    }

    [AvaloniaFact]
    public async Task CancelAndChoosingAnotherProfile_RemoveNothing()
    {
        var (_, viewModel) = await OpenWithTwoSetsAsync();
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);
        keep.CancelCommand.Execute(null);

        Assert.Null(keep.Preview.Title);
        Assert.Empty(keep.Rows);

        await keep.PlanCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);

        Assert.Null(keep.Preview.Title);
        Assert.False(keep.CanRemove);
        Assert.Equal(4, viewModel.Library.Mods.Count);
    }

    private static KeepEachRow Set(KeepEachViewModel keep, string title) => keep.Rows.Single(r => r.Title.StartsWith(title, StringComparison.Ordinal));

    private static Task PickOtherCopyAsync(KeepEachViewModel keep, string title)
    {
        var row = Set(keep, title);
        return PickAsync(keep, row, row.Choices.First(c => c.ModId is not null && c != row.Pick));
    }

    private static Task KeepAllAsync(KeepEachViewModel keep, string title) => PickAsync(keep, Set(keep, title), Set(keep, title).Choices[^1]);

    private static async Task PickAsync(KeepEachViewModel keep, KeepEachRow row, KeepChoice choice)
    {
        row.Pick = choice;
        await keep.ReplanCommand.ExecutionTask!;
    }

    private Task ImportAsync(MainWindowViewModel viewModel, string fileName, string file, string content) =>
        _scenario.ImportAsync(viewModel, Path.Combine(_downloads, fileName), (file, content));

    private async Task ImportAddInAsync(MainWindowViewModel viewModel, string fileName, string content)
    {
        const string Manifest = """<?xml version="1.0"?><AddInItem UID="zeta_uid" Name="Zeta" /> """;
        await _scenario.ImportAsync(
            viewModel,
            Path.Combine(_downloads, fileName),
            ("Manifest.xml", Manifest),
            ("Contents/addins/zeta_uid/module/data.erf", content));
    }

    private async Task SelectSecondProfileAsync(MainWindowViewModel viewModel)
    {
        var other = MockUnixSupport.Path(@"D:\Elsewhere\BioWare\Dragon Age");
        _host.FileSystem.AddDirectory(GamePaths.OverrideDirectory(other));
        viewModel.Profiles.NewProfileName = "Second";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        var paths = _host.Resolve<ProfilePathsViewModel>();
        await paths.LoadAsync("Second", TestContext.Current.CancellationToken);
        _host.Picker.Enqueue(other);
        await paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Documents);
        await viewModel.SelectProfileAsync("Second", TestContext.Current.CancellationToken);
    }

    private async Task<(Views.MainWindow Window, MainWindowViewModel ViewModel)> OpenWithTwoSetsAsync()
    {
        var opened = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(opened.ViewModel);
        await ImportAsync(opened.ViewModel, "Hair Pack Copy.zip", "hair_pack.dds", "DDS hair");
        await ImportAsync(opened.ViewModel, "Eyes.zip", "eyes.dds", "DDS eyes");
        await ImportAsync(opened.ViewModel, "Eyes Copy.zip", "eyes.dds", "DDS eyes");
        Assert.Equal(2, opened.ViewModel.Library.CopySets);
        return opened;
    }
}
