using System.IO.Abstractions.TestingHelpers;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.Core.Mods.Conflicts;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>What the first test on a real library left to fix before a release (P2.6-64 to 66).</summary>
public sealed class BeforeReleaseScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public BeforeReleaseScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ADownloadThatFails_StaysOnTheCard_AndTheOneThatWorkedGoes()
    {
        var folder = MockUnixSupport.Path(@"E:\Get");
        _host.KnownFolders.Downloads = folder;
        var (_, viewModel) = await _scenario.OpenAsync();
        var downloads = viewModel.ModsTab.NewDownloads;
        await downloads.Settings.TurnOnCommand.ExecuteAsync(null);
        _scenario.SaveZip(Path.Combine(folder, "Blue Hair-1-1-0.zip"), ("blue.erf", "x"));
        var broken = Path.Combine(folder, "Red Hair-2-1-0.zip");
        _scenario.SaveZip(broken, ("red.erf", "x"));
        await downloads.LookAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, downloads.Rows.Count);
        _host.FileSystem.AddFile(broken, new MockFileData("not a zip any more"));

        await downloads.AddAllCommand.ExecuteAsync(null);

        Assert.Equal("Red Hair", Assert.Single(downloads.Rows).Title);
        Assert.Equal("Blue Hair", Assert.Single(viewModel.Library.Mods).Title);
    }

    [Fact]
    public void ManyFilesWithTheSameModsAndRisk_AreOneRow_AndAFewStayAsTheyAre()
    {
        var names = new Dictionary<string, string> { ["fixpack"] = "Fixpack", ["zdf"] = "ZDF", ["other"] = "Other" };
        List<PackedConflict> packed =
        [
            .. Enumerable.Range(0, 41).Select(i => new PackedConflict($"talk_{i:00}.dlg", ["fixpack", "zdf"])),
            .. Enumerable.Range(0, 2).Select(i => new PackedConflict($"look_{i}.dds", ["fixpack", "zdf"])),
            new PackedConflict("one.dlg", ["fixpack", "other"]),
        ];

        var rows = PackedRows.For(packed, names);

        Assert.Equal(4, rows.Count);
        var set = rows[0];
        Assert.Equal("Fixpack, ZDF", set.FileName);
        Assert.StartsWith("41 files, such as talk_00.dlg, talk_01.dlg, talk_02.dlg.", set.Detail, StringComparison.Ordinal);
        Assert.Contains("A quest could break.", set.Detail, StringComparison.Ordinal);
        Assert.Equal(SharedFileImpact.CouldBreakQuests, set.Impact);
        Assert.Contains(rows, r => r.FileName == "one.dlg");
        Assert.Contains(rows, r => r.FileName == "look_0.dds");
    }

    [Fact]
    public void MoreRowsThanFit_SayHowManyMore()
    {
        var packed = Enumerable.Range(0, 60).Select(i => new PackedConflict($"file_{i}.dds", [$"a{i}", $"b{i}"])).ToList();

        var rows = PackedRows.For(packed, new Dictionary<string, string>());

        Assert.Equal(51, rows.Count);
        Assert.Equal("10 more", rows[^1].FileName);
    }

    [AvaloniaFact]
    public async Task TurningOffAModWithNoPartInAClash_WarnsAboutNothing_AndDetailsSayWhatGoes()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var downloads = Path.Combine(_host.KnownFolders.MyDocuments, "..", "Downloads");
        await _scenario.ImportAsync(viewModel, Path.Combine(downloads, "Left.zip"), ("shared.dds", "left"));
        await _scenario.ImportAsync(viewModel, Path.Combine(downloads, "Right.zip"), ("shared.dds", "right"));
        await _scenario.ImportAsync(viewModel, Path.Combine(downloads, "Tiny.zip"), ("tiny.dds", "tiny"));
        foreach (var name in new[] { "Left", "Right", "Tiny" })
        {
            await _scenario.ActivateAsync(viewModel, name);
        }

        var activation = viewModel.ModsTab.Activation;
        await activation.DeactivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Tiny"));

        Assert.DoesNotContain(activation.Preview.Notes, n => n.Contains("shared.dds", StringComparison.Ordinal));
        Assert.Equal("Turning off 1: Tiny", activation.Preview.Lines[0]);
        activation.CancelPlanCommand.Execute(null);

        await activation.DeactivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Right"));
        Assert.Equal("Turning off 1: Right", activation.Preview.Lines[0]);
    }

    [AvaloniaFact]
    public async Task TurningOnAModThatClashes_StillWarns()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var downloads = Path.Combine(_host.KnownFolders.MyDocuments, "..", "Downloads");
        await _scenario.ImportAsync(viewModel, Path.Combine(downloads, "Left.zip"), ("shared.dds", "left"));
        await _scenario.ImportAsync(viewModel, Path.Combine(downloads, "Right.zip"), ("shared.dds", "right"));
        await _scenario.ActivateAsync(viewModel, "Left");

        var activation = viewModel.ModsTab.Activation;
        await activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == "Right"));

        Assert.Contains(activation.Preview.Notes, n => n.Contains("shared.dds", StringComparison.Ordinal));
        Assert.Equal("Turning on 1: Right", activation.Preview.Lines[0]);
    }
}
