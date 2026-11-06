using System.IO.Abstractions.TestingHelpers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using DAORework.App.ViewModels;
using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Library;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;
using static DAORework.App.Tests.KeepEachScenario;

namespace DAORework.App.Tests;

/// <summary><i>Keep one of each</i> clears every set of copies from one preview (ADR-0039).</summary>
public sealed class KeepEachScreenTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;
    private readonly KeepEachScenario _sets;

    public KeepEachScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
        _sets = new KeepEachScenario(_scenario);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task AnEntryThatIsOnlyAReadme_IsOfferedAndCleared_AndTheModStaysOn()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Fantasy.zip"), ("fantasy.dds", "DDS"), ("readme.txt", "words"));
        await _scenario.ActivateAsync(viewModel, "Fantasy");
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "left", "Fantasy.zip"), ("readme.txt", "words"));

        // Take them over used to leave an entry like this on, owning no file. Nothing can make one now, so the record says it.
        var token = TestContext.Current.CancellationToken;
        var documents = _scenario.DocumentsDirectory;
        var leftover = (await _host.Resolve<ListLibrary>().ExecuteAsync(token)).Value.Single(e => e.Mod.Files.Count == 1).Mod.Id;
        var records = _host.Resolve<IDeploymentRecordStore>();
        var record = (await records.LoadAsync(documents, token)).Value;
        await records.SaveAsync(documents, record with { ActiveModIds = [.. record.ActiveModIds, leftover] }, token);
        var activation = viewModel.ModsTab.Activation;
        await activation.RefreshActiveAsync(token);
        await viewModel.Library.LoadAsync(token);
        Assert.Equal(2, activation.ActiveModIds.Count);
        var keep = viewModel.ModsTab.KeepEach;

        Assert.All(viewModel.Library.Mods, m => Assert.NotNull(m.Copy));
        Assert.Equal(1, viewModel.Library.CopiesToClear);

        await keep.PlanCommand.ExecuteAsync(null);
        Assert.True(keep.CanRemove);
        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        var left = Assert.Single(viewModel.Library.Mods);
        Assert.True(left.IsActive);
        Assert.Contains("2 files", left.Details, StringComparison.Ordinal);
        Assert.Equal("DDS", _scenario.DeployedContent("fantasy.dds"));
    }

    [AvaloniaFact]
    public async Task TheButton_ShowsOnlyWhileThereAreCopies()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var button = window.GetLogicalDescendants().OfType<Button>().First(b => b.Name == "KeepEachButton");
        Assert.Equal(0, viewModel.Library.CopySets);
        Assert.False(button.IsVisible);

        await _sets.ImportAsync(viewModel, "Hair Pack Copy.zip", "hair_pack.dds", "DDS hair");

        Assert.Equal(1, viewModel.Library.CopySets);
        Assert.True(button.IsVisible);
    }

    [AvaloniaFact]
    public async Task OnePreview_ClearsEverySet_AndChangesNothingInTheGame()
    {
        var (_, viewModel) = await _sets.OpenAsync();
        await _scenario.ActivateAsync(viewModel, "Eyes Copy");
        var backups = viewModel.ModsTab.Changes.Backups.Count;
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);

        Assert.Equal("Keep one of each", keep.Preview.Title);
        Assert.StartsWith("Removes 2 copies of 2 mods from your Library", keep.Preview.Summary, StringComparison.Ordinal);
        Assert.Equal("Remove 2 copies", keep.RemoveLabel);
        Assert.Contains(keep.Preview.Notes, n => n.StartsWith("Removed copies go to the Recycle Bin.", StringComparison.Ordinal));
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
        var (_, viewModel) = await _sets.OpenAsync();
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
        var (_, viewModel) = await _sets.OpenAsync();
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
        Assert.DoesNotContain(keep.Preview.Notes, n => n.StartsWith("Removed copies go to the Recycle Bin.", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task CopiesThatArentTheSame_ComeFirst_AndSayWhatWouldGo()
    {
        var (_, viewModel) = await _sets.OpenAsync();
        await ImportAddInAsync(viewModel, "Zeta One.dazip", "version one");
        await ImportAddInAsync(viewModel, "Zeta Two.dazip", "version two");
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);

        Assert.Equal(3, keep.Rows.Count);
        Assert.Contains("Zeta", keep.Rows[0].Title, StringComparison.Ordinal);
        Assert.StartsWith("These copies aren't the same", keep.Rows[0].Note, StringComparison.Ordinal);
        Assert.Null(keep.Rows[1].Note);
        Assert.Contains("1 mod has copies that aren't the same, so it comes first and stays unless you pick one.", keep.Preview.Notes);
    }

    [AvaloniaFact]
    public async Task ASetOnInTwoProfiles_StaysAsItIs_AndTheOthersGo()
    {
        var (_, viewModel) = await _sets.OpenAsync();
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
        var (_, viewModel) = await _sets.OpenAsync();
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
        var (_, viewModel) = await _sets.OpenAsync();
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
        var (_, viewModel) = await _sets.OpenAsync();
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
}
