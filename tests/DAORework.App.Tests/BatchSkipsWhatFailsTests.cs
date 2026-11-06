using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Planning;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A batch goes on without what fails, says who and why, and no exception reaches the player (P2.6-53).</summary>
public sealed class BatchSkipsWhatFailsTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public BatchSkipsWhatFailsTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task AnUnexpectedException_ShowsNoneOfItsText_AndSaysWhatToDo()
    {
        var page = new Throwing();

        await page.RunAsync();

        Assert.True(page.IsError);
        Assert.DoesNotContain("same key", page.StatusMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Unexpected error", page.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("support bundle", page.StatusMessage, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task Manage_OneOfABatchCantBeManaged_TheRestGoAhead_AndItSaysWhich()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var count = handInstalled.Candidates.Count;
        var loose = handInstalled.Candidates.Single(c => c.NeedsName);
        handInstalled.SelectAllCommand.Execute(null);

        await handInstalled.AdoptSelectedCommand.ExecuteAsync(null);

        var adoption = handInstalled.Adoption;
        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.StartsWith($"Manage {count - 1} mods", adoption.PlanTitle, StringComparison.Ordinal);
        Assert.StartsWith($"Left out: {loose.Candidate!.Name}.", adoption.Preview.Notes[0], StringComparison.Ordinal);
        Assert.Contains("need a name", adoption.Preview.Notes[0], StringComparison.Ordinal);
        Assert.True(adoption.ApplyCommand.CanExecute(null));

        await adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.Contains($"{loose.Candidate.Name} needs a look: ", adoption.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(count - 1, viewModel.Library.Mods.Count(m => m.IsActive));
        Assert.Contains(handInstalled.Candidates, c => c.NeedsName);
    }

    [AvaloniaFact]
    public async Task Manage_AFileCantBeMovedPartway_ThatModIsLeftOut_AndTheRestComeBackAsAPreview()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        var adoption = handInstalled.Adoption;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var count = handInstalled.Candidates.Count;
        handInstalled.SelectAllCommand.Execute(null);
        adoption.LooseFilesName = "Loose fixes";
        await handInstalled.AdoptSelectedCommand.ExecuteAsync(null);
        var textures = viewModel.Library.Mods.Single(m => m.Title == "Textures");
        _host.FileSystem.AddFile(ModFolder(textures.Id), new MockFileData("a file where its folder goes"));

        await adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.StartsWith($"Manage {count - 1} mods", adoption.PlanTitle, StringComparison.Ordinal);
        Assert.StartsWith("Left out: textures.", adoption.Preview.Notes[0], StringComparison.Ordinal);
        Assert.False(viewModel.ModsTab.Changes.ChangeUnfinished);
        Assert.DoesNotContain(viewModel.Library.Mods, m => m.IsActive);
        _host.FileSystem.RemoveFile(ModFolder(textures.Id));

        await adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.Contains("textures needs a look: ", adoption.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(count - 1, viewModel.Library.Mods.Count(m => m.IsActive));
    }

    [AvaloniaFact]
    public async Task Manage_AModThatRunsInsideTheGame_CanBeLeftOut_AndTheRestGoAhead()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        var adoption = handInstalled.Adoption;
        await _scenario.ImportAsync(viewModel, MockUnixSupport.Path(@"C:\Users\Me\Downloads\Overlay.zip"), ("overlay.dll", "program"));
        var overlay = Assert.Single(viewModel.Library.Mods);
        var path = "packages/core/override/Overlay";
        var found = new AdoptionCandidate("override:Overlay", AdoptionCandidateKind.OverrideFolder, "Overlay", path, [new HandInstalledFile($"{path}/overlay.dll", 7)])
        {
            LibraryMatch = new LibraryMatch(overlay.Id, "Overlay", 0),
        };
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var textures = handInstalled.Candidates.Single(c => c.Title == "textures");

        await adoption.PlanAdoptionAsync([HandInstalledModRow.From(found), textures], TestContext.Current.CancellationToken);

        Assert.False(adoption.ApplyCommand.CanExecute(null));
        Assert.True(adoption.CanLeaveOut);
        Assert.Equal("Leave out Overlay", adoption.LeaveOutLabel);

        await adoption.LeaveOutCommand.ExecuteAsync(null);

        Assert.False(adoption.CanLeaveOut);
        Assert.Empty(adoption.Preview.Problems);
        Assert.Equal("Manage textures", adoption.PlanTitle);
        Assert.StartsWith("Left out: Overlay.", adoption.Preview.Notes[0], StringComparison.Ordinal);
        Assert.True(adoption.ApplyCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task TurnOn_AModsLibraryFileIsGonePartway_ThatModIsLeftOut_AndTheRestComeBackAsAPreview()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportAsync(viewModel, MockUnixSupport.Path(@"C:\Users\Me\Downloads\Blue.zip"), ("blue.dds", "blue"));
        await _scenario.ImportAsync(viewModel, MockUnixSupport.Path(@"C:\Users\Me\Downloads\Red.zip"), ("red.dds", "red"));
        var activation = viewModel.ModsTab.Activation;
        await activation.PlanChangeAsync(ModChangeKind.Activate, [.. viewModel.Library.Mods], TestContext.Current.CancellationToken);
        var red = viewModel.Library.Mods.Single(m => m.Title == "Red");
        _host.FileSystem.RemoveFile(Path.Combine(_host.LibraryDirectory, red.Id, "red.dds"));

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.Equal("Turn on Blue", activation.Preview.Title);
        Assert.Equal("Left out: Red. One of its files couldn't be placed. It stays in your Library, off.", activation.Preview.Notes[0]);
        Assert.Contains(red.Id, activation.Preview.Lines[0], StringComparison.Ordinal);
        Assert.False(viewModel.ModsTab.Changes.ChangeUnfinished);

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.StartsWith("Turned on 1 mod. Red needs a look: ", activation.StatusMessage, StringComparison.Ordinal);
        Assert.True(viewModel.Library.Mods.Single(m => m.Title == "Blue").IsActive);
        Assert.False(viewModel.Library.Mods.Single(m => m.Title == "Red").IsActive);
    }

    [AvaloniaFact]
    public async Task TurnOn_OneModAloneFailsPartway_StaysUnfinished_ForHistoryToSettle()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var activation = viewModel.ModsTab.Activation;
        var hair = Assert.Single(viewModel.Library.Mods);
        await activation.PlanChangeAsync(ModChangeKind.Activate, [hair], TestContext.Current.CancellationToken);
        _host.FileSystem.RemoveFile(Path.Combine(_host.LibraryDirectory, hair.Id, "hair_pack.dds"));

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.True(activation.IsError);
        Assert.Null(activation.PlanTitle);
    }

    [AvaloniaFact]
    public async Task Add_OneOfSeveralDownloadsFails_TheRestAreAdded_AndItSaysWhich()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var good = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Good.zip");
        var bad = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Bad.zip");
        _scenario.SaveZip(good, ("good.dds", "good"));
        _host.FileSystem.AddFile(bad, new MockFileData("not a zip"));

        await viewModel.Library.ImportPathsAsync([bad, good], TestContext.Current.CancellationToken);

        Assert.True(viewModel.Library.IsError);
        Assert.StartsWith("Added 1 mod. Bad.zip needs a look: ", viewModel.Library.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(viewModel.Library.Mods, m => m.Title == "Good");
    }

    [AvaloniaFact]
    public async Task Add_OneDownloadThrows_TheRestAreStillAdded()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var good = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Good.zip");
        _scenario.SaveZip(good, ("good.dds", "good"));

        await viewModel.Library.ImportPathsAsync(["bad\0name.zip", good], TestContext.Current.CancellationToken);

        Assert.True(viewModel.Library.IsError);
        Assert.StartsWith("Added 1 mod.", viewModel.Library.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(viewModel.Library.Mods, m => m.Title == "Good");
    }

    private string ModFolder(string modId) => Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "DAORework", modId);

    private sealed class Throwing() : PageViewModel(NullLogger.Instance)
    {
        public Task RunAsync() => GuardAsync(_ => throw new ArgumentException("An item with the same key has already been added."), CancellationToken.None);
    }
}
