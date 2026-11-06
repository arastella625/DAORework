using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A preview and the files in the override folder DAORework doesn't manage (P2.6-20), read from the disk.</summary>
public sealed class UnmanagedCopiesScreenTests : IDisposable
{
    private const string Warned = "which DAORework does not manage";

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public UnmanagedCopiesScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TurnOn_AModWhoseFileIsThereAlready_SaysNothing()
    {
        var viewModel = await OpenWithLooseHairAsync("DDS hair");

        var notes = await PreviewHairPackAsync(viewModel);

        Assert.DoesNotContain(notes, n => n.Contains(Warned, StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task TurnOn_AModWhoseFileDiffersFromTheOneThere_Warns()
    {
        var viewModel = await OpenWithLooseHairAsync("something else");

        var notes = await PreviewHairPackAsync(viewModel);

        Assert.Contains(notes, n => n.Contains(Warned, StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task TurnOn_AnotherMod_DoesNotMentionTheOneAlreadyOn()
    {
        var viewModel = await OpenWithLooseHairAsync("something else");
        await _scenario.ActivateHairPackAsync(viewModel);
        await _scenario.ImportAsync(viewModel, MockUnixSupport.Path(@"C:\Users\Me\Downloads\Eyes.zip"), ("eyes.dds", "eyes"));

        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Eyes"));

        Assert.DoesNotContain(viewModel.ModsTab.Activation.Preview.Notes, n => n.Contains(Warned, StringComparison.Ordinal));
    }

    private async Task<MainWindowViewModel> OpenWithLooseHairAsync(string content)
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        _host.FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "Old Hair", "hair_pack.dds"), new MockFileData(content));
        await _scenario.ImportHairPackAsync(viewModel);
        return viewModel;
    }

    private static async Task<IReadOnlyList<string>> PreviewHairPackAsync(MainWindowViewModel viewModel)
    {
        var activation = viewModel.ModsTab.Activation;
        await activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack"));
        Assert.False(activation.IsError, activation.StatusMessage);
        return activation.Preview.Notes;
    }
}
