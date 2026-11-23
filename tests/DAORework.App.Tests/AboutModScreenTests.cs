using System.Collections.ObjectModel;
using Avalonia.Headless.XUnit;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using Xunit;

namespace DAORework.App.Tests;

/// <summary><i>About this mod</i> (P2.10-05, ADR-0050 §3): the page a row's name opens, in place of the list.</summary>
public sealed class AboutModScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;

    public AboutModScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task TheName_OpensItsPage_WhoseSwitchWorks_AndBackReturnsToTheList()
    {
        var (window, viewModel) = await _scenario.OpenAsync();
        await _scenario.ImportHairPackAsync(viewModel);
        var about = viewModel.ModsTab.About;

        about.OpenCommand.Execute(viewModel.Library.Mods.Single(m => m.Title == "Hair Pack"));
        ModsTabScenario.Show<AboutModPanel>(window);
        Screenshots.Save(window, "about-this-mod");

        Assert.True(about.IsOpen);
        Assert.Equal("Override mod", about.Kind);
        Assert.False(about.ShowsDetails); // Kind, files and notes wait under Details.

        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(about.Mod!);
        await viewModel.ModsTab.Activation.ApplyCommand.ExecuteAsync(null);

        Assert.False(viewModel.ModsTab.Activation.IsError, viewModel.ModsTab.Activation.StatusMessage);
        Assert.True(about.Mod!.IsActive); // The page followed the Library to the mod's new row.
        about.BackCommand.Execute(null);
        Assert.False(about.IsOpen);
    }

    [Fact]
    public void ThePage_FollowsTheSameModToItsNewRow_AndClosesWhenItHasGone()
    {
        var mods = new ObservableCollection<LibraryModRow> { Row("long-hair") };
        var about = new AboutModViewModel(mods);
        about.OpenCommand.Execute(mods[0]);

        var reread = Row("long-hair");
        mods[0] = reread;
        Assert.Same(reread, about.Mod);

        mods.Clear();
        Assert.False(about.IsOpen);
    }

    private static LibraryModRow Row(string id) =>
        LibraryModRow.From(
            new LibraryEntry(new LibraryMod(id, "Long Hair", ModKind.Override, []), "long-hair.zip", "sha", DateTimeOffset.UnixEpoch),
            Result.Failure<DownloadCheck>(new Error(DiagnosticCodes.ModImportInvalid, "Not compared.")));
}
