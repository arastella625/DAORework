using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.Core.Mods;
using DAORework.Core.Mods.Library;
using DAORework.Infrastructure.Persistence;
using Xunit;
using static DAORework.App.Tests.KeepEachScenario;

namespace DAORework.App.Tests;

/// <summary>The app leads the clearing of copies, and a mod that is only part of another counts as one (ADR-0039).</summary>
public sealed class KeepEachPartsTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;
    private readonly KeepEachScenario _sets;

    public KeepEachPartsTests()
    {
        _scenario = new ModsTabScenario(_host);
        _sets = new KeepEachScenario(_scenario);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TheNextStep_OpensTheFilledInPreview_AndGoesOnceTheCopiesDo()
    {
        var (_, viewModel) = await _sets.OpenAsync(seedInstall: false);
        var card = viewModel.ModsTab.NextMove;
        Assert.Equal(NextMoveKind.ClearCopies, card.Kind);
        Assert.Equal("2 mods are in your Library more than once", card.Title);
        Assert.Equal("Keep one of each", card.Button);

        await card.TakeCommand.ExecuteAsync(null);

        var keep = viewModel.ModsTab.KeepEach;
        Assert.Equal(2, keep.Rows.Count);
        Assert.True(keep.CanRemove);
        Assert.Equal(4, viewModel.Library.Mods.Count);

        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.NotEqual(NextMoveKind.ClearCopies, card.Kind);
    }

    [AvaloniaFact]
    public async Task APartThatIsOnBesideItsWhole_GoesAndTheGameKeepsItsFiles()
    {
        var (_, viewModel) = await OpenWithAPartAsync();
        await _scenario.ActivateAsync(viewModel, "Tattoos");
        await _scenario.ActivateAsync(viewModel, "Tat-toos");
        Assert.Equal(2, viewModel.ModsTab.Activation.ActiveModIds.Count);
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);

        var row = Assert.Single(keep.Rows);
        Assert.Equal("Tat-toos, on, newest, only part", row.Choices[1].Label);
        Assert.Equal("The others are only part of this one, so nothing is lost.", row.Note);
        Assert.True(keep.CanRemove, string.Join(" | ", keep.Preview.Problems.Select(p => p.Title)));

        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Equal("Tattoos", Assert.Single(viewModel.Library.Mods).Title);
        Assert.Single(viewModel.ModsTab.Activation.ActiveModIds);
        Assert.Equal("DDS a", _scenario.DeployedContent("tattoo_a.dds"));
        Assert.Equal("DDS b", _scenario.DeployedContent("tattoo_b.dds"));
    }

    [AvaloniaFact]
    public async Task ACopyThatIsOnAndOwnsNoFile_LeavesTheRecord_WithTheGameUntouched()
    {
        var (_, viewModel) = await _sets.OpenAsync(seedInstall: false);
        await _scenario.ActivateAsync(viewModel, "Eyes");
        await _scenario.ActivateAsync(viewModel, "Eyes Copy");
        var backups = viewModel.ModsTab.Changes.Backups.Count;
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);

        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.StartsWith("eyes-copy-", Assert.Single(viewModel.ModsTab.Activation.ActiveModIds), StringComparison.Ordinal);
        Assert.Equal(backups, viewModel.ModsTab.Changes.Backups.Count);
        Assert.Equal("DDS eyes", _scenario.DeployedContent("eyes.dds"));
    }

    [AvaloniaFact]
    public async Task AChangeAfterThePreview_IsNotLost_WhenTheSwapMovesNoFile()
    {
        var (_, viewModel) = await _sets.OpenAsync(seedInstall: false);
        await _sets.ImportAsync(viewModel, "Lips.zip", "lips.dds", "DDS lips");
        await _scenario.ActivateAsync(viewModel, "Eyes");
        await _scenario.ActivateAsync(viewModel, "Eyes Copy");
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);

        await _scenario.ActivateAsync(viewModel, "Lips");
        await keep.RemoveCopiesCommand.ExecuteAsync(null);

        Assert.True(keep.IsError);
        Assert.Contains("Nothing was removed: What's on has changed", keep.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(3, viewModel.ModsTab.Activation.ActiveModIds.Count);
        Assert.Equal(5, viewModel.Library.Mods.Count);
    }

    [AvaloniaFact]
    public async Task EveryRowOfASet_SaysSo()
    {
        var (_, viewModel) = await OpenWithAPartAsync();

        Assert.All(viewModel.Library.Mods, m => Assert.Contains("2 copies", m.Glance, StringComparison.Ordinal));
        Assert.Contains(viewModel.Library.Mods.Single(m => m.Title == "Tat-toos").Notes, n => n.StartsWith("Everything in this mod is also in Tattoos.", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task CopiesThatReallyDiffer_NeedTheirOwnChoice_AndAreNotANextStep()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await ImportAddInAsync(viewModel, "Zeta One.dazip", "version one");
        await ImportAddInAsync(viewModel, "Zeta Two.dazip", "version two");
        Assert.Equal(1, viewModel.Library.CopySets);
        Assert.Equal(0, viewModel.Library.CopiesToClear);
        Assert.NotEqual(NextMoveKind.ClearCopies, viewModel.ModsTab.NextMove.Kind);
        var keep = viewModel.ModsTab.KeepEach;

        await keep.PlanCommand.ExecuteAsync(null);

        var row = Assert.Single(keep.Rows);
        Assert.Null(row.Pick!.ModId);
        Assert.Equal("These copies aren't the same, so they all stay unless you pick one.", row.Note);
        Assert.False(keep.CanRemove);

        await PickOtherCopyAsync(keep, row.Title);

        Assert.StartsWith("These copies aren't the same. Only the ones being removed have", row.Note, StringComparison.Ordinal);
        Assert.True(keep.CanRemove);
    }

    [AvaloniaFact]
    public async Task ASwapThatIsBlocked_SaysWhy_AndRemovesNothing()
    {
        var (_, viewModel) = await _sets.OpenAsync();
        await _sets.ImportAsync(viewModel, "Lips.zip", "lips.dds", "DDS lips");
        await _scenario.ActivateAsync(viewModel, "Hair Pack Copy");
        await _scenario.ActivateAsync(viewModel, "Lips");
        MakeAProgram(viewModel.Library.Mods.Single(m => m.Title == "Lips").Id);
        var keep = viewModel.ModsTab.KeepEach;
        await keep.PlanCommand.ExecuteAsync(null);
        Assert.True(keep.CanRemove);

        await PickOtherCopyAsync(keep, "Hair Pack");

        Assert.Contains(keep.Preview.Problems, p => p.Title.StartsWith("Lips", StringComparison.Ordinal));
        Assert.False(keep.CanRemove);
        Assert.False(keep.RemoveCopiesCommand.CanExecute(null));
        Assert.Equal(5, viewModel.Library.Mods.Count);
    }

    /// <summary>A mod the game can't take any more, so a plan that keeps it on is blocked.</summary>
    private void MakeAProgram(string modId)
    {
        var path = Path.Combine(_host.LibraryDirectory, modId + ".mod.json");
        var entry = JsonSerializer.Deserialize<LibraryEntry>(_host.FileSystem.File.ReadAllText(path), JsonDefaults.Options)!;
        _host.FileSystem.File.WriteAllText(path, JsonSerializer.Serialize(entry with { Mod = entry.Mod with { Kind = ModKind.GameProgram } }, JsonDefaults.Options));
    }

    private async Task<(Views.MainWindow Window, MainWindowViewModel ViewModel)> OpenWithAPartAsync()
    {
        var opened = await _scenario.OpenAsync();
        await _scenario.ImportAsync(opened.ViewModel, Path.Combine(_downloads, "Tattoos.zip"), ("tattoo_a.dds", "DDS a"), ("tattoo_b.dds", "DDS b"));
        await _scenario.ImportAsync(opened.ViewModel, Path.Combine(_downloads, "Tat-toos.zip"), ("tattoo_a.dds", "DDS a"));
        return opened;
    }

    private async Task ImportAddInAsync(MainWindowViewModel viewModel, string fileName, string content)
    {
        const string Manifest = """<?xml version="1.0"?><AddInItem UID="zeta_uid" Name="Zeta" /> """;
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, fileName), ("Manifest.xml", Manifest), ("Contents/addins/zeta_uid/module/data.erf", content));
    }
}
