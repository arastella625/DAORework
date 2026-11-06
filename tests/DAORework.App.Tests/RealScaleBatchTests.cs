using Avalonia.Headless.XUnit;
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
}
