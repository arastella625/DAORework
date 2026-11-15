using System.Collections.ObjectModel;
using System.IO.Abstractions.TestingHelpers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Mods.AddIns;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Each add-in's Priority on Mods, and the chip for add-ins that share a file (P2.5-23, ADR-0026 §2).</summary>
public sealed class AddInPriorityScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task ModsTab_AddInsSharingAFile_AreTagged_AndTheChipShowsOnlyThem()
    {
        var scenario = new ModsTabScenario(_host);
        _host.SeedRealisticInstall();
        var addIns = GamePaths.AddInsDirectory(scenario.DocumentsDirectory);
        _host.FileSystem.AddFile(
            GamePaths.AddInsListFile(scenario.DocumentsDirectory),
            new MockFileData("""
                <AddInsList>
                  <AddInItem UID="Extra_Dog_Slot" Enabled="1" Priority="100" ExtendedModuleUID="Single Player"><Title DefaultText="Extra Dog Slot v1.41" /></AddInItem>
                  <AddInItem UID="Camp_Tweaks" Enabled="1" Priority="109" ExtendedModuleUID="Single Player"><Title DefaultText="Camp Tweaks" /></AddInItem>
                  <AddInItem UID="Dye_Kit" Enabled="1" Priority="100" ExtendedModuleUID="Single Player"><Title DefaultText="Dye Kit" /></AddInItem>
                </AddInsList>
                """));
        _host.FileSystem.AddFile(Path.Combine(addIns, "Extra_Dog_Slot", "core", "override", "party_base.ncs"), new MockFileData(new byte[64]));
        _host.FileSystem.AddFile(Path.Combine(addIns, "Camp_Tweaks", "core", "override", "party_base.ncs"), new MockFileData(new byte[64]));
        _host.FileSystem.AddFile(Path.Combine(addIns, "Dye_Kit", "core", "override", "dyes.gda"), new MockFileData(new byte[64]));

        var (window, viewModel) = await scenario.OpenAsync(seedInstall: false);
        var filter = viewModel.ModsTab.Filter;

        Assert.False(viewModel.ModsTab.AddIns.IsError, viewModel.ModsTab.AddIns.StatusMessage);
        var dog = Row(viewModel, "Extra Dog Slot v1.41");
        var camp = Row(viewModel, "Camp Tweaks");
        Assert.Equal("Priority 100 · wins", dog.PriorityMark?.Text);
        Assert.Equal("Priority 109 · loses", camp.PriorityMark?.Text);
        Assert.Equal("Priority 100", Row(viewModel, "Dye Kit").PriorityMark?.Text);
        Assert.StartsWith("party_base.ncs is in both Extra Dog Slot v1.41 and Camp Tweaks. The game uses Extra Dog Slot v1.41's copy", dog.PriorityMark!.Sentences[0], StringComparison.Ordinal);
        Assert.True(filter.HasSharingAddIns);
        Assert.Equal("Add-ins sharing files (2)", filter.SharingAddInsLabel);

        filter.ShowOnlyCommand.Execute(ModShowFilter.SharingAddIns);
        Assert.Equal([camp, dog], filter.VisibleFound);
        ModsTabScenario.Show<ModsListPanel>(window);
        window.Height = 1100; // Down from the chips to both rows.
        window.UpdateLayout();
        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), t => t.Name == "PriorityMark" && t.IsEffectivelyVisible && t.Text == "Priority 109 · loses");
        Screenshots.Save(window, "48-addin-priority");

        // Look again reads the priorities too, so a file taken out by hand stops being said.
        _host.FileSystem.File.Delete(Path.Combine(addIns, "Camp_Tweaks", "core", "override", "party_base.ncs"));
        await viewModel.HandInstalled.ScanCommand.ExecuteAsync(null);
        Assert.False(filter.HasSharingAddIns);
        Assert.Equal(ModShowFilter.All, filter.Show);
        Assert.Equal("Priority 100", Row(viewModel, "Extra Dog Slot v1.41").PriorityMark?.Text);
    }

    [Fact]
    public void Filter_ALibraryAddIn_GetsItsMarkByUid_AndKeepsItWhenTheListIsReadAgain()
    {
        var mods = new ObservableCollection<LibraryModRow> { Row("dog", "EXTRA_DOG_SLOT"), Row("hair", null) };
        var filter = new ModListFilter(mods, []);
        var mark = new AddInMark(100, Wins: true, Loses: false, ["party_base.ncs is in both..."]);

        filter.AddInMarks = new Dictionary<string, AddInMark>(StringComparer.OrdinalIgnoreCase) { ["Extra_Dog_Slot"] = mark };
        mods[0] = Row("dog", "EXTRA_DOG_SLOT");

        Assert.Same(mark, mods[0].PriorityMark);
        Assert.Null(mods[1].PriorityMark);
        filter.ShowOnlyCommand.Execute(ModShowFilter.SharingAddIns);
        Assert.Equal(["dog"], filter.VisibleMods.Select(m => m.Id));
    }

    private static HandInstalledModRow Row(MainWindowViewModel viewModel, string title) =>
        viewModel.HandInstalled.Candidates.Single(c => c.Title == title);

    private static LibraryModRow Row(string id, string? addInUid) =>
        LibraryModRow.From(
            new LibraryEntry(new LibraryMod(id, id, addInUid is null ? ModKind.Override : ModKind.AddIn, [], addInUid), id, id, DateTimeOffset.UnixEpoch),
            Result.Failure<DownloadCheck>(new Error("X", "x")));
}
