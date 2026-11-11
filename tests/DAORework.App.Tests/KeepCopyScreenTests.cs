using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.App.Tests;

/// <summary><i>Keep this one</i> removes a mod's other copies from the Library (ADR-0039).</summary>
public sealed class KeepCopyScreenTests : IDisposable
{
    private static readonly string _copy = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Hair Pack Copy.zip");

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public KeepCopyScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task KeepThisOne_RemovesTheOtherCopy_OnlyOnceApplied()
    {
        var (window, viewModel) = await OpenWithTwoCopiesAsync();
        var keep = viewModel.ModsTab.KeepCopy;
        var folder = LibraryFolder("hair-pack-copy");

        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));

        Assert.Equal("Keep Hair Pack", keep.Preview.Title);
        Assert.StartsWith("Removes Hair Pack Copy from your Library", keep.Preview.Summary, StringComparison.Ordinal);
        Assert.Contains("added from Hair Pack Copy.zip", Assert.Single(keep.Preview.Lines), StringComparison.Ordinal);
        Assert.Equal("Remove the other copy", keep.RemoveLabel);
        Assert.True(keep.CanRemove);
        Assert.True(_host.FileSystem.Directory.Exists(folder));
        Row(viewModel, "Hair Pack").IsExpanded = true;
        window.UpdateLayout();
        Screenshots.Save(window, "46-keep-this-one");

        await keep.RemoveOthersCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Equal("Hair Pack", Assert.Single(viewModel.Library.Mods).Title);
        Assert.False(_host.FileSystem.Directory.Exists(folder));
        Assert.Null(Row(viewModel, "Hair Pack").Copy);
    }

    [AvaloniaFact]
    public async Task AnAddInCopyThatIsOn_IsSwappedInPlace_WithItsEntryKept()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await ImportAddInAsync(viewModel, "Blue One.dazip", "version one");
        await ImportAddInAsync(viewModel, "Blue Two.dazip", "version two");
        var rows = viewModel.Library.Mods.OrderBy(m => m.AddedAt).ThenBy(m => m.Details, StringComparer.Ordinal).ToList();
        Assert.All(rows, r => Assert.NotNull(r.Copy));
        var activation = viewModel.ModsTab.Activation;
        await activation.ActivateCommand.ExecuteAsync(rows.First(r => r.Details.Contains("Blue One", StringComparison.Ordinal)));
        await activation.ApplyCommand.ExecuteAsync(null);
        Assert.False(activation.IsError, activation.StatusMessage);
        var keep = viewModel.ModsTab.KeepCopy;

        await keep.PlanKeepCommand.ExecuteAsync(rows.First(r => r.Details.Contains("Blue Two", StringComparison.Ordinal)));

        Assert.True(keep.CanRemove, string.Join(" | ", keep.Preview.Problems.Select(p => p.Title)));
        Assert.Contains(keep.Preview.Notes, n => n.StartsWith("These copies aren't the same.", StringComparison.Ordinal));

        await keep.RemoveOthersCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Single(viewModel.Library.Mods);
        Assert.Single(activation.ActiveModIds);
        var deployed = Path.Combine(_scenario.DocumentsDirectory, "AddIns", "blue_uid", "module", "data.erf");
        Assert.Equal("version two", _host.FileSystem.File.ReadAllText(deployed));
        Assert.Contains("blue_uid", _host.FileSystem.File.ReadAllText(Path.Combine(_scenario.DocumentsDirectory, "Settings", "AddIns.xml")), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task ChoosingAnotherProfile_ClosesThePreview()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        var keep = viewModel.ModsTab.KeepCopy;
        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));

        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);

        Assert.Null(keep.Preview.Title);
        Assert.False(keep.CanRemove);
    }

    [AvaloniaFact]
    public async Task ACopyTurnedOnAfterThePreview_IsNotRemoved()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        var keep = viewModel.ModsTab.KeepCopy;
        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));

        await _scenario.ActivateAsync(viewModel, "Hair Pack Copy");
        await keep.RemoveOthersCommand.ExecuteAsync(null);

        Assert.True(keep.IsError);
        Assert.Contains(DiagnosticCodes.ModCopyStillOn, keep.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(2, viewModel.Library.Mods.Count);
        Assert.True(Row(viewModel, "Hair Pack Copy").IsActive);
    }

    [AvaloniaFact]
    public async Task ARemovedCopy_GoesToTheRecycleBin_WithItsRecord()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        var keep = viewModel.ModsTab.KeepCopy;

        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));

        Assert.EndsWith("goes to the Recycle Bin, and comes back when you empty it.", keep.Preview.Summary, StringComparison.Ordinal);

        await keep.RemoveOthersCommand.ExecuteAsync(null);

        Assert.Equal(2, _host.RecycleBin.Sent.Count);
        Assert.EndsWith(".mod.json", _host.RecycleBin.Sent[0], StringComparison.Ordinal);
        Assert.Contains("hair-pack-copy", Path.GetFileName(_host.RecycleBin.Sent[1]), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task WithNoRecycleBin_ThePreviewSaysRemovingIsForGood()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        _host.RecycleBin.IsAvailable = false;
        var keep = viewModel.ModsTab.KeepCopy;

        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));

        Assert.Contains(keep.Preview.Notes, n => n.StartsWith("Removing can't be undone.", StringComparison.Ordinal));
        Assert.Contains("You get back", keep.Preview.Summary, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task Cancel_RemovesNothing()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        var keep = viewModel.ModsTab.KeepCopy;

        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));
        keep.CancelCommand.Execute(null);

        Assert.Null(keep.Preview.Title);
        Assert.Equal(2, viewModel.Library.Mods.Count);
    }

    [AvaloniaFact]
    public async Task ACopyThatIsOn_GivesItsPlaceToTheOneKept_AndUndoAsksForItBack()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        await _scenario.ImportAsync(viewModel, MockUnixSupport.Path(@"C:\Users\Me\Downloads\Blue Eyes.zip"), ("blue_eyes.dds", "DDS eyes"));
        await _scenario.ActivateAsync(viewModel, "Hair Pack Copy");
        await _scenario.ActivateAsync(viewModel, "Blue Eyes");
        var keep = viewModel.ModsTab.KeepCopy;

        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));

        Assert.Contains("Hair Pack Copy is on, so this one takes its place.", keep.Preview.Notes);
        Assert.Contains(keep.Preview.Notes, n => n.Contains("History can't go back", StringComparison.Ordinal));

        await keep.RemoveOthersCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Equal(["Blue Eyes", "Hair Pack"], viewModel.Library.Mods.Select(m => m.Title).Order());
        var active = viewModel.ModsTab.Activation.ActiveModIds;
        Assert.StartsWith("hair-pack-", active[0], StringComparison.Ordinal);
        Assert.DoesNotContain("copy", active[0], StringComparison.Ordinal);
        Assert.StartsWith("blue-eyes-", active[1], StringComparison.Ordinal);
        Assert.Equal("DDS hair", _scenario.DeployedContent("hair_pack.dds"));

        var changes = viewModel.ModsTab.Changes;
        await changes.RollBackToCommand.ExecuteAsync(changes.Backups[0]);
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.True(changes.IsError);
        Assert.Contains(DiagnosticCodes.ModUndoNeedsRemovedMod, changes.StatusMessage, StringComparison.Ordinal);
        Assert.StartsWith("Couldn't undo it: Hair pack copy is no longer in your Library, and this change goes back to it.", changes.StatusMessage, StringComparison.Ordinal);
        Assert.Matches(@"Add the same download again, or put back hair-pack-copy-\w+ and hair-pack-copy-\w+\.mod\.json if they're in the Recycle Bin", changes.StatusMessage);
        Assert.Equal("DDS hair", _scenario.DeployedContent("hair_pack.dds"));

        await viewModel.Library.ImportPathsAsync([_copy], TestContext.Current.CancellationToken);
        await changes.RollBackToCommand.ExecuteAsync(changes.Backups[0]);
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.False(changes.IsError, changes.StatusMessage);
        Assert.Contains("hair-pack-copy", viewModel.ModsTab.Activation.ActiveModIds[0], StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task KeepingTheCopyThatIsOn_ChangesNothingInTheGame()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        await _scenario.ActivateAsync(viewModel, "Hair Pack");
        var keep = viewModel.ModsTab.KeepCopy;
        var backups = viewModel.ModsTab.Changes.Backups.Count;

        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));
        await keep.RemoveOthersCommand.ExecuteAsync(null);

        Assert.False(keep.IsError, keep.StatusMessage);
        Assert.Single(viewModel.Library.Mods);
        Assert.Equal(backups, viewModel.ModsTab.Changes.Backups.Count);
        Assert.True(Row(viewModel, "Hair Pack").IsActive);
    }

    [AvaloniaFact]
    public async Task ACopyOnInAnotherProfile_StopsIt()
    {
        var (_, viewModel) = await OpenWithTwoCopiesAsync();
        await _scenario.ActivateAsync(viewModel, "Hair Pack Copy");
        var other = MockUnixSupport.Path(@"D:\Elsewhere\BioWare\Dragon Age");
        _host.FileSystem.AddDirectory(GamePaths.OverrideDirectory(other));
        viewModel.Profiles.NewProfileName = "Second";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        var paths = _host.Resolve<ProfilePathsViewModel>();
        await paths.LoadAsync("Second", TestContext.Current.CancellationToken);
        _host.Picker.Enqueue(other);
        await paths.BrowseCommand.ExecuteAsync(ProfilePathKind.Documents);
        await viewModel.SelectProfileAsync("Second", TestContext.Current.CancellationToken);
        var keep = viewModel.ModsTab.KeepCopy;

        await keep.PlanKeepCommand.ExecuteAsync(Row(viewModel, "Hair Pack"));

        Assert.False(keep.CanRemove);
        Assert.Contains(keep.Preview.Problems, p => p.Title.StartsWith("Hair Pack Copy is on in", StringComparison.Ordinal));
        Assert.Equal(2, viewModel.Library.Mods.Count);
    }

    private async Task ImportAddInAsync(MainWindowViewModel viewModel, string fileName, string content)
    {
        const string Manifest = """<?xml version="1.0"?><AddInItem UID="blue_uid" Name="Blue" /> """;
        await _scenario.ImportAsync(
            viewModel,
            Path.Combine(MockUnixSupport.Path(@"C:\Users\Me\Downloads"), fileName),
            ("Manifest.xml", Manifest),
            ("Contents/addins/blue_uid/module/data.erf", content));
    }

    private static LibraryModRow Row(MainWindowViewModel viewModel, string title) => viewModel.Library.Mods.Single(m => m.Title == title);

    private async Task<(Views.MainWindow Window, MainWindowViewModel ViewModel)> OpenWithTwoCopiesAsync()
    {
        var opened = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(opened.ViewModel);
        await _scenario.ImportAsync(opened.ViewModel, _copy, ("hair_pack.dds", "DDS hair"));
        Assert.All(opened.ViewModel.Library.Mods, m => Assert.NotNull(m.Copy));
        return opened;
    }

    private string LibraryFolder(string prefix) =>
        _host.FileSystem.AllDirectories.Single(d => Path.GetFileName(d).StartsWith(prefix, StringComparison.Ordinal));
}
