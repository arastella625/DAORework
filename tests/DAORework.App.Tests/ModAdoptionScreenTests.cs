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

    [AvaloniaFact]
    public async Task Adopt_AnOverrideFolder_ShowsThePlan_ThenTakesItOver()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        var handInstalled = viewModel.HandInstalled;
        await handInstalled.ScanCommand.ExecuteAsync(null);
        var textures = handInstalled.Candidates.Single(c => c.Title == "textures");

        await handInstalled.Adoption.AdoptCommand.ExecuteAsync(textures);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Equal("Adopt textures", handInstalled.Adoption.PlanTitle);
        Assert.Contains(handInstalled.Adoption.PlanLines, l => l.StartsWith("  Move      1 file", StringComparison.Ordinal));
        Assert.True(handInstalled.Adoption.ApplyCommand.CanExecute(null));
        Assert.False(Assert.Single(viewModel.Library.Mods).IsActive);
        Assert.True(_host.FileSystem.File.Exists(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "textures", "hair_01.dds")));
        ModsTabScenario.Show<HandInstalledPanel>(window);
        Screenshots.Save(window, "16-adopt-plan");

        await handInstalled.Adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.StartsWith("Adopted textures: ", handInstalled.Adoption.StatusMessage, StringComparison.Ordinal);
        var adopted = Assert.Single(viewModel.Library.Mods);
        Assert.Equal("textures", adopted.Title);
        Assert.True(adopted.IsActive);
        Assert.Contains("Adopted from packages/core/override/textures.", adopted.Notes);
        Assert.DoesNotContain(handInstalled.Candidates, c => c.Title == "textures");
        Assert.Single(viewModel.ModsTab.Changes.Backups);
        Assert.False(_host.FileSystem.File.Exists(Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "textures", "hair_01.dds")));
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
        Assert.Equal("Adopt Loose files in override", handInstalled.Adoption.PlanTitle);
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
        Assert.StartsWith($"Adopt {count} mods: ", handInstalled.Adoption.PlanTitle, StringComparison.Ordinal);
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
