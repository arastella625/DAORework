using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>P2.8-05 (ADR-0033 §3): a mod that runs inside the game, turned on from the Mods tab after the player's tick.</summary>
public sealed class GameProgramScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public GameProgramScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
        _host.Scanner.Result = new MalwareScan(MalwareScanStatus.NoThreatFound, "No threats found by Defender.");
    }

    private string PlacedDll => Path.Combine(AppTestHost.InstallDirectory, "bin_ship", "dinput8.dll");

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TurnOn_ShowsItsOwnPreview_AndApplyWaitsForTheTick()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await ImportDaoLibAsync(viewModel);
        var programs = viewModel.ModsTab.Programs;
        var mod = viewModel.Library.Mods.Single();

        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(mod);

        Assert.False(programs.IsError, programs.StatusMessage);
        Assert.Equal("Turn on DAO Lib", programs.Title);
        Assert.Equal("Copies 1 file into your game folder.", programs.Summary);
        Assert.True(programs.NeedsTrust);
        Assert.Contains(programs.Notes, n => n.StartsWith("1 file outside its bin_ship folder", StringComparison.Ordinal));
        Assert.Null(viewModel.ModsTab.Activation.Preview.Title);
        Assert.False(programs.ApplyCommand.CanExecute(null));
        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "37-game-program-preview");

        programs.IsTrusted = true;
        Assert.True(programs.ApplyCommand.CanExecute(null));
        await programs.ApplyCommand.ExecuteAsync(null);

        Assert.False(programs.IsError, programs.StatusMessage);
        Assert.True(_host.FileSystem.File.Exists(PlacedDll));
        Assert.True(viewModel.Library.Mods.Single().IsActive);
    }

    [AvaloniaFact]
    public async Task TurnOn_AModThatNeedsThe4GbPatch_WarnsInThePreview()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        _host.FileSystem.AddFile(GamePaths.GameExecutable(AppTestHost.InstallDirectory), new MockFileData(FixFixtures.Executable()));
        await _scenario.ImportAsync(
            viewModel,
            MockUnixSupport.Path(@"C:\Users\Me\Downloads\Native Controller Framework.zip"),
            ("bin_ship/dinput8.dll", Dll()),
            ("bin_ship/DAControllerFramework.ini", "[pad]"));
        var programs = viewModel.ModsTab.Programs;

        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single());

        Assert.False(programs.IsError, programs.StatusMessage);
        var need = Assert.Single(programs.Needs);
        Assert.StartsWith("Native Controller Framework needs the 4 GB patch, its author says.", need, StringComparison.Ordinal);
        Assert.True(programs.NeedsTrust);
        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "38-game-program-needs");
    }

    [AvaloniaFact]
    public async Task TurnOff_NeedsNoTick_AndRemovesTheDll()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await ImportDaoLibAsync(viewModel);
        var programs = viewModel.ModsTab.Programs;
        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single());
        programs.IsTrusted = true;
        await programs.ApplyCommand.ExecuteAsync(null);

        await viewModel.ModsTab.Activation.DeactivateCommand.ExecuteAsync(viewModel.Library.Mods.Single());

        Assert.Equal("Turn off DAO Lib", programs.Title);
        Assert.False(programs.NeedsTrust);
        await programs.ApplyCommand.ExecuteAsync(null);
        Assert.False(_host.FileSystem.File.Exists(PlacedDll));
        Assert.False(viewModel.Library.Mods.Single().IsActive);
    }

    [AvaloniaFact]
    public async Task TurnOn_ShowsInHistory_AndUndoThereTurnsTheRowOff()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await ImportDaoLibAsync(viewModel);
        var programs = viewModel.ModsTab.Programs;
        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single());
        programs.IsTrusted = true;
        await programs.ApplyCommand.ExecuteAsync(null);
        var history = viewModel.Fixes.Status;

        var change = Assert.Single(history.Backups);
        Assert.Equal("Turned on DAO Lib", change.Change);

        await history.RollBackToCommand.ExecuteAsync(change);
        await history.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.False(history.IsError, history.StatusMessage);
        Assert.False(_host.FileSystem.File.Exists(PlacedDll));
        Assert.Empty(programs.ActiveModIds);
        Assert.False(viewModel.Library.Mods.Single().IsActive);
    }

    [AvaloniaFact]
    public async Task TurnOff_ACopyOnInTheDocumentsFolderFromBefore_GoesTheUsualWay()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await ImportDaoLibAsync(viewModel);
        var token = TestContext.Current.CancellationToken;
        var mod = viewModel.Library.Mods.Single();
        var records = _host.Resolve<IDeploymentRecordStore>();
        var record = (await records.LoadAsync(_scenario.DocumentsDirectory, token)).Value;
        await records.SaveAsync(_scenario.DocumentsDirectory, record with { ActiveModIds = [mod.Id] }, token);
        await viewModel.ModsTab.Activation.RefreshActiveAsync(token);
        Assert.True(mod.IsActive);

        await viewModel.ModsTab.Activation.DeactivateCommand.ExecuteAsync(mod);

        Assert.Equal("Turn off DAO Lib", viewModel.ModsTab.Activation.Preview.Title);
        Assert.Null(viewModel.ModsTab.Programs.Title);
        Assert.False(viewModel.ModsTab.Programs.IsError, viewModel.ModsTab.Programs.StatusMessage);
    }

    [AvaloniaFact]
    public async Task TurnOnSelected_LeavesItOutAndNamesIt()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await ImportDaoLibAsync(viewModel);
        foreach (var row in viewModel.Library.Mods)
        {
            row.IsSelected = true;
        }

        await viewModel.ModsTab.Selection.ActivateSelectedCommand.ExecuteAsync(null);

        var preview = viewModel.ModsTab.Activation.Preview;
        Assert.Equal("Turn on Hair Pack", preview.Title);
        Assert.Contains(preview.Notes, n => n.StartsWith("Left out: DAO Lib. It runs its own program inside the game", StringComparison.Ordinal));
        Assert.Null(viewModel.ModsTab.Programs.Title);
        Assert.False(_host.FileSystem.File.Exists(PlacedDll));
    }

    [AvaloniaFact]
    public async Task TurnOn_NotScanned_SaysSoAndShowsNoPreview()
    {
        _host.Scanner.Result = new MalwareScan(MalwareScanStatus.NotScanned, "No antivirus answered.");
        var (_, viewModel) = await _scenario.OpenAsync();
        await ImportDaoLibAsync(viewModel);
        var programs = viewModel.ModsTab.Programs;

        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single());

        Assert.True(programs.IsError);
        Assert.Contains("no antivirus could check it", programs.StatusMessage, StringComparison.Ordinal);
        Assert.Null(programs.Title);
    }

    [AvaloniaFact]
    public async Task TheRow_SaysTurningItOnAsksFirst()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await ImportDaoLibAsync(viewModel);

        var mod = viewModel.Library.Mods.Single();

        Assert.Contains("runs in the game", mod.Details, StringComparison.Ordinal);
        Assert.Contains("It runs its own program inside the game (bin_ship/dinput8.dll). Turning it on asks first whether you trust it.", mod.Notes);
    }

    /// <summary>A made-up 32-bit DLL header whose bytes are all below 0x80, so the scenario's text zip keeps them as they are.</summary>
    private static string Dll()
    {
        var header = new char[128];
        header[0] = 'M';
        header[1] = 'Z';
        header[0x3C] = (char)0x40;
        "PE\0\0".CopyTo(0, header, 0x40, 4);
        header[0x44] = (char)0x4C;
        header[0x45] = (char)0x01;
        header[0x44 + 18] = (char)0x02;
        header[0x44 + 19] = (char)0x21;
        return new string(header);
    }

    private Task ImportDaoLibAsync(MainWindowViewModel viewModel) =>
        _scenario.ImportAsync(viewModel, MockUnixSupport.Path(@"C:\Users\Me\Downloads\DAO Lib.zip"), ("bin_ship/dinput8.dll", Dll()), ("readme.txt", "hi"));
}
