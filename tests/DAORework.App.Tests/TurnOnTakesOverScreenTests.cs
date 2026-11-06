using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Avalonia.Headless.XUnit;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Turning a mod on takes over the identical file already in the game, so no duplicate is made (ADR-0037).</summary>
public sealed class TurnOnTakesOverScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public TurnOnTakesOverScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TurnOn_AModAlreadyInTheGame_MovesItsFile_LeavesOneCopy_AndUndoPutsItBack()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var loose = Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "Old Hair", "hair_pack.dds");
        _host.FileSystem.AddFile(loose, new MockFileData("DDS hair"));
        await _scenario.ImportHairPackAsync(viewModel);
        var activation = viewModel.ModsTab.Activation;

        await activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack"));

        Assert.StartsWith("Moves 1 file", activation.Preview.Summary, StringComparison.Ordinal);
        Assert.True(_host.FileSystem.File.Exists(loose));

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.False(_host.FileSystem.File.Exists(loose));
        Assert.Single(_scenario.DeployedHair());
        Assert.Equal("DDS hair", _scenario.DeployedContent("hair_pack.dds"));
        Assert.True(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack").IsActive);

        var changes = viewModel.ModsTab.Changes;
        await changes.RollBackToCommand.ExecuteAsync(changes.Backups[0]);
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.False(changes.IsError, changes.StatusMessage);
        Assert.Equal("DDS hair", _host.FileSystem.File.ReadAllText(loose));
        Assert.Single(_scenario.DeployedHair());
    }

    [AvaloniaFact]
    public async Task AModInTheLibraryAndTheGame_ShowsOnce_AndTurningItOnClearsTheNote()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var overrideFolder = GamePaths.OverrideDirectory(_scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Hair Pack", "hair_pack.dds"), new MockFileData("DDS hair"));
        await _scenario.ImportHairPackAsync(viewModel);
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
        var filter = viewModel.ModsTab.Filter;

        Assert.DoesNotContain(filter.VisibleFound, f => f.Title == "Hair Pack");
        Assert.True(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack").IsInGame);

        await _scenario.ActivateHairPackAsync(viewModel);

        Assert.DoesNotContain(viewModel.HandInstalled.Candidates, f => f.Title == "Hair Pack");
        Assert.False(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack").IsInGame);
    }

    [AvaloniaFact]
    public async Task TurnOn_AModWhoseFileDiffers_LeavesTheOtherFileWhereItIs()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var loose = Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "Old Hair", "hair_pack.dds");
        _host.FileSystem.AddFile(loose, new MockFileData("something else"));
        await _scenario.ImportHairPackAsync(viewModel);

        await _scenario.ActivateHairPackAsync(viewModel);

        Assert.Equal("something else", _host.FileSystem.File.ReadAllText(loose));
        Assert.Equal(2, _scenario.DeployedHair().Count);
    }

    [AvaloniaFact]
    public async Task TurnOn_AFileALiveVortexPlaced_IsLeftForVortex_AndTheModIsCopied()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var loose = PlacedByVortex(stagingHoldsTheMod: true);
        await _scenario.ImportHairPackAsync(viewModel);

        await _scenario.ActivateHairPackAsync(viewModel);

        Assert.True(_host.FileSystem.File.Exists(loose));
        Assert.Equal(2, _scenario.DeployedHair().Count);
    }

    [AvaloniaFact]
    public async Task TurnOn_AFileVortexLeftBehind_IsTakenOver()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var loose = PlacedByVortex(stagingHoldsTheMod: false);
        await _scenario.ImportHairPackAsync(viewModel);

        await _scenario.ActivateHairPackAsync(viewModel);

        Assert.False(_host.FileSystem.File.Exists(loose));
        Assert.Single(_scenario.DeployedHair());
    }

    private string PlacedByVortex(bool stagingHoldsTheMod)
    {
        var overrideFolder = GamePaths.OverrideDirectory(_scenario.DocumentsDirectory);
        var staging = Path.Combine(Path.GetDirectoryName(_host.PreferencesFile)!, "Vortex", "dragonage", "mods");
        var loose = Path.Combine(overrideFolder, "Hair", "hair_pack.dds");
        _host.FileSystem.AddFile(loose, new MockFileData("DDS hair"));
        _host.FileSystem.AddDirectory(staging);
        if (stagingHoldsTheMod)
        {
            _host.FileSystem.AddDirectory(Path.Combine(staging, "Hair Pack-1-0"));
        }

        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "vortex.deployment.json"), new MockFileData(JsonSerializer.Serialize(new
        {
            targetPath = overrideFolder,
            stagingPath = staging,
            files = new[] { new { relPath = "Hair\\hair_pack.dds", source = "Hair Pack-1-0" } },
        })));
        return loose;
    }
}
