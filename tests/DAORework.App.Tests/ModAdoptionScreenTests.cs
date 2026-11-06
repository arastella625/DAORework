using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Adopting mods installed by hand from the Mods tab (P2.5-03, P3-04, P2.5-13): the plan first, then Apply, as <c>mods adopt</c> does.</summary>
public sealed class ModAdoptionScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public ModAdoptionScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    private static string Morph(string hair) => $"""<morph_config><hairs><human_male><resource name="{hair}" /></human_male></hairs></morph_config>""";

    [AvaloniaFact]
    public async Task Adopt_AnOverrideFolder_ShowsThePlan_ThenTakesItOver()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var textures = handInstalled.Candidates.Single(c => c.Title == "textures");

        await handInstalled.Adoption.AdoptCommand.ExecuteAsync(textures);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Equal("Manage textures", handInstalled.Adoption.PlanTitle);
        Assert.Contains(handInstalled.Adoption.PlanLines, l => l.StartsWith("  Move      1 file", StringComparison.Ordinal));
        Assert.True(handInstalled.Adoption.ApplyCommand.CanExecute(null));
        Assert.False(Assert.Single(viewModel.Library.Mods).IsActive);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "textures", "hair_01.dds")));
        ModsTabScenario.Show<HandInstalledPanel>(window);
        Screenshots.Save(window, "16-adopt-plan");

        await handInstalled.Adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.StartsWith("Now managing textures.", handInstalled.Adoption.StatusMessage, StringComparison.Ordinal);
        var adopted = Assert.Single(viewModel.Library.Mods);
        Assert.Equal("Textures", adopted.Title);
        Assert.True(adopted.IsActive);
        Assert.Contains("Was in packages/core/override/textures before DAORework managed it.", adopted.Notes);
        Assert.DoesNotContain(handInstalled.Candidates, c => c.Title == "textures");
        Assert.Single(viewModel.ModsTab.Changes.Backups);
        Assert.False(_host.FileSystem.File.Exists(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "textures", "hair_01.dds")));
    }

    [AvaloniaFact]
    public async Task Adopt_TwoModsWithDifferentFilesOfOneName_ShowsWhoWins_AndSwapChangesWhatIsApplied()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var overrideFolder = GamePaths.OverrideDirectory(_scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Alpha", "look.dds"), new MockFileData("alpha"));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Beta", "look.dds"), new MockFileData("beta"));
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var adoption = handInstalled.Adoption;

        await adoption.PlanAdoptionAsync([handInstalled.Candidates.Single(c => c.Title == "Alpha"), handInstalled.Candidates.Single(c => c.Title == "Beta")], TestContext.Current.CancellationToken);

        var row = Assert.Single(adoption.WhoWins);
        Assert.Equal("Beta wins over Alpha", row.Title);
        Assert.Equal("1 texture or model", row.Kinds);
        Assert.True(row.CanSwap);
        ModsTabScenario.Show<HandInstalledPanel>(window);
        Screenshots.Save(window, "adopt-who-wins");

        await adoption.SwapCommand.ExecuteAsync(row);

        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.Equal("Alpha wins over Beta", Assert.Single(adoption.WhoWins).Title);
        Assert.Equal("alpha", _host.FileSystem.File.ReadAllText(Path.Combine(overrideFolder, "Alpha", "look.dds")));

        await adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.Equal("alpha", _scenario.DeployedContent("look.dds"));
        Assert.Empty(adoption.WhoWins);
    }

    [AvaloniaFact]
    public async Task Manage_ALeftoverOfAModWithACombinedFile_DoesNotStop()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");
        await _scenario.ImportAsync(viewModel, Path.Combine(downloads, "Long Hair.zip"), (GamePaths.ChargenMorphFileName, Morph("long")), ("long.dds", "DDS long"));
        await _scenario.ImportAsync(viewModel, Path.Combine(downloads, "Short Hair.zip"), (GamePaths.ChargenMorphFileName, Morph("short")), ("short.dds", "DDS short"));
        await _scenario.ActivateAsync(viewModel, "Long Hair");
        await _scenario.ActivateAsync(viewModel, "Short Hair");
        var left = Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "Old Long");
        _host.FileSystem.AddFile(Path.Combine(left, GamePaths.ChargenMorphFileName), new MockFileData(Morph("long")));
        _host.FileSystem.AddFile(Path.Combine(left, "long.dds"), new MockFileData("DDS long"));
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);

        await handInstalled.Adoption.AdoptCommand.ExecuteAsync(handInstalled.Candidates.Single(c => c.Title == "Old Long"));

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);

        await handInstalled.Adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.False(_host.FileSystem.File.Exists(Path.Combine(left, "long.dds")));
        Assert.Equal(2, viewModel.ModsTab.Activation.ActiveModIds.Count);
    }

    [AvaloniaFact]
    public async Task Manage_OneModFoundTwice_TurnsItOnOnce_AndClearsTheOtherCopy()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var overrideFolder = GamePaths.OverrideDirectory(_scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Copy A", "hair_pack.dds"), new MockFileData("DDS hair"));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Copy B", "hair_pack.dds"), new MockFileData("DDS hair"));
        await _scenario.ImportHairPackAsync(viewModel);
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);

        await handInstalled.Adoption.PlanAdoptionAsync([.. handInstalled.Candidates.Where(c => c.Title.StartsWith("Copy", StringComparison.Ordinal))], TestContext.Current.CancellationToken);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);

        await handInstalled.Adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Single(viewModel.ModsTab.Activation.ActiveModIds);
        Assert.Single(_scenario.DeployedHair());
    }

    [AvaloniaFact]
    public async Task Swap_WithALeftoverInTheSameChange_StillSwaps_AndStillClearsIt()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);
        var overrideFolder = GamePaths.OverrideDirectory(_scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Old Pack", "hair_pack.dds"), new MockFileData("DDS hair"));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Red", "shared.dds"), new MockFileData("DDS red"));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Blue", "shared.dds"), new MockFileData("DDS blue"));
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var adoption = handInstalled.Adoption;
        var rows = handInstalled.Candidates.Where(c => c.Title is "Blue" or "Old Pack" or "Red").OrderBy(c => c.Title, StringComparer.Ordinal).ToList();
        await adoption.PlanAdoptionAsync(rows, TestContext.Current.CancellationToken);
        Assert.False(adoption.IsError, adoption.StatusMessage);
        var row = Assert.Single(adoption.WhoWins);
        Assert.True(row.CanSwap);

        await adoption.SwapCommand.ExecuteAsync(row);

        Assert.False(adoption.IsError, adoption.StatusMessage);
        await adoption.ApplyCommand.ExecuteAsync(null);
        Assert.False(adoption.IsError, adoption.StatusMessage);
        Assert.False(_host.FileSystem.File.Exists(Path.Combine(overrideFolder, "Old Pack", "hair_pack.dds")));
        Assert.Equal("DDS blue", _scenario.DeployedContent("shared.dds"));
    }

    [AvaloniaFact]
    public async Task Manage_AModAlreadyOn_ClearsItsLeftoverCopy_AndUndoPutsItBack()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var loose = Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "Hair Pack", "hair_pack.dds");
        await _scenario.ImportHairPackAsync(viewModel);
        await _scenario.ActivateHairPackAsync(viewModel);
        _host.FileSystem.AddFile(loose, new MockFileData("DDS hair"));
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);

        await handInstalled.Adoption.AdoptCommand.ExecuteAsync(handInstalled.Candidates.Single(c => c.Title == "Hair Pack"));

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.True(handInstalled.Adoption.ApplyCommand.CanExecute(null));
        Assert.True(_host.FileSystem.File.Exists(loose));

        await handInstalled.Adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.False(_host.FileSystem.File.Exists(loose));
        Assert.Equal("DDS hair", _scenario.DeployedContent("hair_pack.dds"));
        Assert.True(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack").IsActive);

        var changes = viewModel.ModsTab.Changes;
        await changes.RollBackToCommand.ExecuteAsync(changes.Backups[0]);
        await changes.ConfirmRollbackCommand.ExecuteAsync(null);

        Assert.False(changes.IsError, changes.StatusMessage);
        Assert.True(_host.FileSystem.File.Exists(loose));
    }

    [AvaloniaFact]
    public async Task Adopt_LooseFiles_NeedAName()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var loose = handInstalled.Candidates.Single(c => c.NeedsName);

        await handInstalled.Adoption.AdoptCommand.ExecuteAsync(loose);

        Assert.True(handInstalled.Adoption.IsError);
        Assert.EndsWith($"({DiagnosticCodes.ModAdoptNameRequired})", handInstalled.Adoption.StatusMessage, StringComparison.Ordinal);
        Assert.Null(handInstalled.Adoption.PlanTitle);

        handInstalled.Adoption.LooseFilesName = "Loose fixes";
        await handInstalled.Adoption.AdoptCommand.ExecuteAsync(loose);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Equal("Manage Loose files in override", handInstalled.Adoption.PlanTitle);
        Assert.True(handInstalled.Adoption.ApplyCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task Adopt_Cancelled_MovesNothingAndKeepsTheLibraryCopy()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var before = handInstalled.Candidates.Select(c => c.Title).ToList();
        await handInstalled.Adoption.AdoptCommand.ExecuteAsync(handInstalled.Candidates.Single(c => c.Title == "textures"));

        handInstalled.Adoption.CancelPlanCommand.Execute(null);

        Assert.Equal("Nothing in the documents folder was changed. The copy planning made stays in the Library.", handInstalled.Adoption.StatusMessage);
        Assert.False(handInstalled.Adoption.ApplyCommand.CanExecute(null));
        Assert.Equal(before, handInstalled.Candidates.Select(c => c.Title));
        Assert.False(Assert.Single(viewModel.Library.Mods).IsActive);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "textures", "hair_01.dds")));
    }

    [AvaloniaFact]
    public async Task AdoptSelected_NeedsATick_AndSelectAllAndClearSetEveryTick()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);

        Assert.False(handInstalled.AdoptSelectedCommand.CanExecute(null));

        handInstalled.SelectAllCommand.Execute(null);

        Assert.All(handInstalled.Candidates, c => Assert.True(c.IsSelected));
        Assert.True(handInstalled.AdoptSelectedCommand.CanExecute(null));

        handInstalled.ClearSelectionCommand.Execute(null);

        Assert.All(handInstalled.Candidates, c => Assert.False(c.IsSelected));
        Assert.False(handInstalled.AdoptSelectedCommand.CanExecute(null));

        handInstalled.Candidates.Single(c => c.Title == "textures").IsSelected = true;

        Assert.True(handInstalled.AdoptSelectedCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task AdoptSelected_AdoptsEveryTickedMod_AsOneChange()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var count = handInstalled.Candidates.Count;
        handInstalled.SelectAllCommand.Execute(null);
        handInstalled.Adoption.LooseFilesName = "Loose fixes";

        await handInstalled.AdoptSelectedCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Equal($"Manage {count} mods", handInstalled.Adoption.PlanTitle);
        Assert.True(handInstalled.Adoption.ApplyCommand.CanExecute(null));
        ModsTabScenario.Show<HandInstalledPanel>(window);
        Screenshots.Save(window, "28-adopt-selected");

        await handInstalled.Adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Empty(handInstalled.Candidates);
        Assert.False(handInstalled.AdoptSelectedCommand.CanExecute(null));
        Assert.Equal(count, viewModel.Library.Mods.Count(m => m.IsActive));
        Assert.Single(viewModel.ModsTab.Changes.Backups);
        Assert.False(_host.FileSystem.File.Exists(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "textures", "hair_01.dds")));
        Assert.False(_host.FileSystem.File.Exists(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "qwinn_fixpack", "fixes.gda")));
    }
}
