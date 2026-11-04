using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Avalonia.Headless.XUnit;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Mods.Handover;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The <i>Move from Vortex</i> card (ADR-0028 §3): copy, purge, then one preview to turn the copies on.</summary>
public sealed class VortexHandoverScreenTests : IDisposable
{
    private const string Blue = "Blue Hair-1-1-0";
    private const string Red = "Red Hair-2-1-0";

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public VortexHandoverScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    private string Override => Path.Combine(_scenario.DocumentsDirectory, "packages", "core", "override");

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Card_NoVortex_StaysHidden()
    {
        var (_, viewModel) = await _scenario.OpenAsync();

        Assert.False(viewModel.ModsTab.Handover.IsShown);
    }

    [AvaloniaFact]
    public async Task Card_WalksThroughCopyPurgeAndTurnOn_WithTheWinnerLast()
    {
        _host.SeedRealisticInstall();
        SeedVortex();
        var (window, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        var handover = viewModel.ModsTab.Handover;

        Assert.Equal(VortexHandoverStep.Copy, handover.Step);
        Assert.Equal(["Blue Hair · 1 file", "Red Hair · 1 file"], handover.Mods);
        ModsTabScenario.Show<VortexHandoverPanel>(window);
        Screenshots.Save(window, "38-move-from-vortex");

        await handover.CopyCommand.ExecuteAsync(null);

        Assert.False(handover.IsError, handover.StatusMessage);
        Assert.StartsWith("Copied 2 mods.", handover.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(VortexHandoverStep.Purge, handover.Step);
        Assert.Equal(2, viewModel.Library.Mods.Count);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(Override, "hair.dds")));

        await handover.CheckAgainCommand.ExecuteAsync(null);
        Assert.Equal(VortexHandoverStep.Purge, handover.Step);

        Purge();
        await handover.CheckAgainCommand.ExecuteAsync(null);
        Assert.Equal(VortexHandoverStep.TurnOn, handover.Step);

        viewModel.ModsTab.Filter.ShowOnlyCommand.Execute(DAORework.App.ViewModels.ModShowFilter.Found);
        await handover.TurnOnAllCommand.ExecuteAsync(null);
        Assert.True(viewModel.ModsTab.Filter.ShowsMods);
        var activation = viewModel.ModsTab.Activation;
        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Equal("Turn on 2 mods: Red Hair, Blue Hair", activation.PlanTitle);

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(VortexHandoverStep.Done, handover.Step);
        Assert.False(handover.IsShown);
        var winner = Assert.Single(_host.FileSystem.AllFiles, f => f.EndsWith("hair.dds", StringComparison.OrdinalIgnoreCase) && f.StartsWith(Override, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("blue", _host.FileSystem.File.ReadAllText(winner));
    }

    [AvaloniaFact]
    public async Task TurnOn_AfterThePurgeLostDlc_PutsItBackAsItWas()
    {
        _host.SeedRealisticInstall();
        SeedVortex();
        var list = GamePaths.AddInsListFile(_scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(list, new MockFileData("""<AddInsList><AddInItem UID="DAO_PRC_EP_1" BioWare="1"><Title DefaultText="Awakening" /></AddInItem></AddInsList>"""));
        var (_, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        var handover = viewModel.ModsTab.Handover;
        await handover.CopyCommand.ExecuteAsync(null);
        Purge();
        _host.FileSystem.File.WriteAllText(list, "<AddInsList />");
        await handover.CheckAgainCommand.ExecuteAsync(null);

        await handover.TurnOnAllCommand.ExecuteAsync(null);
        var activation = viewModel.ModsTab.Activation;

        Assert.Contains(activation.Preview.Notes, w => w.Contains("back in AddIns.xml", StringComparison.Ordinal));
        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Contains("BioWare=\"1\"", _host.FileSystem.File.ReadAllText(list), StringComparison.Ordinal);
        Assert.Equal(VortexHandoverStep.Done, handover.Step);
    }

    private void SeedVortex()
    {
        var staging = Path.Combine(Path.GetDirectoryName(_host.PreferencesFile)!, "Vortex", "dragonage", "mods");
        _host.FileSystem.AddFile(Path.Combine(staging, Blue, "hair.dds"), new MockFileData("blue"));
        _host.FileSystem.AddFile(Path.Combine(staging, Red, "hair.dds"), new MockFileData("red"));
        _host.FileSystem.AddFile(Path.Combine(staging, Red, "red.dds"), new MockFileData("more"));
        _host.FileSystem.AddFile(Path.Combine(Override, "hair.dds"), new MockFileData("blue"));
        _host.FileSystem.AddFile(Path.Combine(Override, "red.dds"), new MockFileData("more"));
        _host.FileSystem.AddFile(Path.Combine(Override, "vortex.deployment.json"), new MockFileData(JsonSerializer.Serialize(new
        {
            targetPath = Override,
            stagingPath = staging,
            files = new[] { new { relPath = "hair.dds", source = Blue }, new { relPath = "red.dds", source = Red } },
        })));
    }

    private void Purge()
    {
        foreach (var file in new[] { "hair.dds", "red.dds", "vortex.deployment.json" })
        {
            _host.FileSystem.File.Delete(Path.Combine(Override, file));
        }
    }
}
