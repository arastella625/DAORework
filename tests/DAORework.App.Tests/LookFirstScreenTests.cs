using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>A download that looks already held is asked about before anything is added (ADR-0039).</summary>
public sealed class LookFirstScreenTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");
    private static readonly string _hairPack = Path.Combine(_downloads, "Hair Pack.zip");

    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public LookFirstScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
        _host.KnownFolders.Downloads = _downloads;
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task AModInTheGame_IsOfferedManageFirst_AndNothingIsAdded()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await FindInTheGameAsync(viewModel, "Old Hair");
        _scenario.SaveZip(_hairPack, ("hair_pack.dds", "DDS hair"));
        var lookFirst = viewModel.ModsTab.LookFirst;

        await viewModel.Library.AddPathsAsync([_hairPack], TestContext.Current.CancellationToken);

        Assert.Empty(viewModel.Library.Mods);
        Assert.Equal("Hair Pack looks like Old Hair, which is already in your game.", Assert.Single(lookFirst.Held).Line);
        Assert.True(lookFirst.CanManage);
        window.UpdateLayout();
        Screenshots.Save(window, "45-look-first");

        await lookFirst.ManageHeldCommand.ExecuteAsync(null);

        Assert.False(lookFirst.IsShown);
        Assert.Equal("Manage Old Hair", viewModel.HandInstalled.Adoption.PlanTitle);
        Assert.DoesNotContain(viewModel.Library.Mods, m => m.Title == "Hair Pack");
    }

    [AvaloniaFact]
    public async Task LooseFiles_TakeTheDownloadsName_SoManageNeedsNoQuestion()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        _host.FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "hair_pack.dds"), new MockFileData("DDS hair"));
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
        var loose = viewModel.HandInstalled.Candidates.Single(c => c.NeedsName).Candidate!.Files;
        _scenario.SaveZip(_hairPack, [.. loose.Select(f => (Path.GetFileName(f.Path), "same"))]);
        var lookFirst = viewModel.ModsTab.LookFirst;

        await viewModel.Library.AddPathsAsync([_hairPack], TestContext.Current.CancellationToken);

        Assert.Equal("Hair Pack looks like it's already in your game, as loose files.", Assert.Single(lookFirst.Held).Line);
        Assert.True(lookFirst.CanManage);

        await lookFirst.ManageHeldCommand.ExecuteAsync(null);

        Assert.False(viewModel.HandInstalled.Adoption.IsError, viewModel.HandInstalled.Adoption.StatusMessage);
        Assert.Equal("Hair Pack", viewModel.HandInstalled.Adoption.LooseFilesName);
    }

    [AvaloniaFact]
    public async Task LooseFiles_ThatHoldOtherMods_AreNotOfferedManage()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var overrideFolder = GamePaths.OverrideDirectory(_scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "hair_pack.dds"), new MockFileData("DDS hair"));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "someone_elses.dds"), new MockFileData("DDS other"));
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
        _scenario.SaveZip(_hairPack, ("hair_pack.dds", "DDS hair"));
        var lookFirst = viewModel.ModsTab.LookFirst;

        await viewModel.Library.AddPathsAsync([_hairPack], TestContext.Current.CancellationToken);

        Assert.Single(lookFirst.Held);
        Assert.False(lookFirst.CanManage);
    }

    [AvaloniaFact]
    public async Task AModInTheLibrary_CanStillBeAddedAnyway()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var update = Path.Combine(_downloads, "Hair Pack v2.zip");
        _scenario.SaveZip(update, ("hair_pack.dds", "DDS hair, new"));
        var lookFirst = viewModel.ModsTab.LookFirst;

        await viewModel.Library.AddPathsAsync([update], TestContext.Current.CancellationToken);

        Assert.Equal("Hair Pack v2 looks like Hair Pack, which is already in your Library.", Assert.Single(lookFirst.Held).Line);
        Assert.False(lookFirst.CanManage);

        await lookFirst.AddAnywayCommand.ExecuteAsync(null);

        Assert.False(lookFirst.IsShown);
        Assert.Equal(2, viewModel.Library.Mods.Count);
    }

    [AvaloniaFact]
    public async Task ADifferentMod_IsAddedStraightAway_WhileTheOneThatLooksHeldWaits()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await FindInTheGameAsync(viewModel, "Old Hair");
        var other = Path.Combine(_downloads, "Blue Eyes.zip");
        _scenario.SaveZip(_hairPack, ("hair_pack.dds", "DDS hair"));
        _scenario.SaveZip(other, ("blue_eyes.dds", "DDS eyes"));
        var lookFirst = viewModel.ModsTab.LookFirst;

        await viewModel.Library.AddPathsAsync([_hairPack, other], TestContext.Current.CancellationToken);

        Assert.Equal("Blue Eyes", Assert.Single(viewModel.Library.Mods).Title);
        Assert.Single(lookFirst.Held);

        lookFirst.CancelCommand.Execute(null);

        Assert.False(lookFirst.IsShown);
        Assert.Single(viewModel.Library.Mods);
    }

    [AvaloniaFact]
    public async Task ANewDownload_IsLookedAtToo()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        _host.FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "Old Hair", "hair.erf"), new MockFileData("ERF"));
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
        var downloads = viewModel.ModsTab.NewDownloads;
        await downloads.Settings.TurnOnCommand.ExecuteAsync(null);
        _scenario.SaveZip(_hairPack, ("hair.erf", "ERF"));
        await downloads.LookAsync(TestContext.Current.CancellationToken);

        await downloads.AddOneCommand.ExecuteAsync(downloads.Rows.Single());

        Assert.Empty(viewModel.Library.Mods);
        Assert.True(viewModel.ModsTab.LookFirst.IsShown);
    }

    private async Task FindInTheGameAsync(MainWindowViewModel viewModel, string folder)
    {
        _host.FileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), folder, "hair_pack.dds"), new MockFileData("DDS hair"));
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
    }
}
