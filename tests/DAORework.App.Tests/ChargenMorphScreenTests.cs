using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>
/// Combining chargenmorphcfg.xml from several mods (P2.5-20, ADR-0025), through the real library, planner and writer.
/// The XML is made up here: a few entries in the file's shape, not the game's own list.
/// </summary>
public sealed class ChargenMorphScreenTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public ChargenMorphScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    private string Combined => Path.Combine(GamePaths.OverrideDirectory(_scenario.DocumentsDirectory), "DAORework", "_combined", GamePaths.ChargenMorphFileName);

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TwoHairMods_Activated_DeployOneCombinedFile_AndDeactivatingOneTakesItAway()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Long Hair.zip"), (GamePaths.ChargenMorphFileName, Morph("hm_har_long")), ("hm_har_long_0.mmh", "mesh"));
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Short Hair.zip"), (GamePaths.ChargenMorphFileName, Morph("hm_har_short")), ("hm_har_short_0.mmh", "mesh"));
        var activation = viewModel.ModsTab.Activation;
        viewModel.ModsTab.Selection.SelectAllCommand.Execute(null);

        await viewModel.ModsTab.Selection.ActivateSelectedCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.EndsWith("Combines chargenmorphcfg.xml from Long Hair and Short Hair: 3 entries.", activation.Preview.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain(activation.Preview.Notes, n => n.Contains(GamePaths.ChargenMorphFileName, StringComparison.Ordinal));
        ModsTabScenario.Show<LibraryPanel>(window);
        Screenshots.Save(window, "30-chargenmorph-combined");

        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        var deployed = _host.FileSystem.File.ReadAllText(Combined);
        Assert.Contains("hm_har_base", deployed, StringComparison.Ordinal);
        Assert.Contains("hm_har_long", deployed, StringComparison.Ordinal);
        Assert.Contains("hm_har_short", deployed, StringComparison.Ordinal);
        Assert.Single(MorphCopies());

        await activation.DeactivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Short Hair"));
        await activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.False(_host.FileSystem.File.Exists(Combined));
        var left = Assert.Single(MorphCopies());
        Assert.Contains("hm_har_long", _host.FileSystem.File.ReadAllText(left), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task ACopyThatCannotBeRead_IsNotCombined_AndThePlanSaysWhich()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Long Hair.zip"), (GamePaths.ChargenMorphFileName, Morph("hm_har_long")));
        await _scenario.ImportAsync(viewModel, Path.Combine(_downloads, "Broken Hair.zip"), (GamePaths.ChargenMorphFileName, "<morph_config><hairs>"));
        var activation = viewModel.ModsTab.Activation;
        viewModel.ModsTab.Selection.SelectAllCommand.Execute(null);

        await viewModel.ModsTab.Selection.ActivateSelectedCommand.ExecuteAsync(null);

        Assert.False(activation.IsError, activation.StatusMessage);
        Assert.DoesNotContain("Combines", activation.Preview.Summary, StringComparison.Ordinal);
        Assert.Contains(activation.Preview.Notes, n => n.Contains("Broken Hair", StringComparison.Ordinal) && n.EndsWith($"({DiagnosticCodes.ModChargenMorphUnreadable})", StringComparison.Ordinal));
        Assert.Contains(activation.Preview.Notes, n => n.StartsWith("chargenmorphcfg.xml is in", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task TwoModsInstalledByHand_AdoptedTogether_AreCombined()
    {
        var overrideFolder = GamePaths.OverrideDirectory(_scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Long Hair", GamePaths.ChargenMorphFileName), new MockFileData(Morph("hm_har_long")));
        _host.FileSystem.AddFile(Path.Combine(overrideFolder, "Short Hair", GamePaths.ChargenMorphFileName), new MockFileData(Morph("hm_har_short")));
        var (_, viewModel) = await _scenario.OpenAsync(seedInstall: false);
        var handInstalled = viewModel.HandInstalled;
        handInstalled.SelectAllCommand.Execute(null);

        await handInstalled.AdoptSelectedCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Contains("Combines chargenmorphcfg.xml from", handInstalled.Adoption.Preview.Summary, StringComparison.Ordinal);

        await handInstalled.Adoption.ApplyCommand.ExecuteAsync(null);

        Assert.False(handInstalled.Adoption.IsError, handInstalled.Adoption.StatusMessage);
        Assert.Equal([Combined], MorphCopies());
        Assert.Contains("hm_har_short", _host.FileSystem.File.ReadAllText(Combined), StringComparison.Ordinal);
        Assert.Single(viewModel.ModsTab.Changes.Backups);
    }

    /// <summary>A small chargenmorphcfg.xml: one base hair every copy shares, and the mod's own.</summary>
    private static string Morph(string hair) => $"""
        <morph_config>
          <hairs>
            <human_male>
              <resource name="hm_har_base" cut="0" />
              <resource name="{hair}" cut="0" />
            </human_male>
          </hairs>
        </morph_config>
        """;

    private List<string> MorphCopies() =>
        [.. _host.FileSystem.AllFiles.Where(f => f.StartsWith(_scenario.DocumentsDirectory, StringComparison.Ordinal) && Path.GetFileName(f) == GamePaths.ChargenMorphFileName)];
}
