using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using Avalonia.Headless.XUnit;
using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The question on Home and the <i>New downloads</i> card (ADR-0034).</summary>
public sealed class NewDownloadsScreenTests : IDisposable
{
    private readonly AppTestHost _host = new();
    private readonly ModsTabScenario _scenario;
    private readonly string _folder = MockUnixSupport.Path(@"E:\Get");

    public NewDownloadsScreenTests()
    {
        _scenario = new ModsTabScenario(_host);
        _host.KnownFolders.Downloads = _folder;
    }

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Off_LooksAtNothing_AndHomeAsksOnce()
    {
        Seed("Blue Hair-1-1-0.zip", "hair.erf");
        var (_, viewModel) = await _scenario.OpenAsync();
        var downloads = viewModel.ModsTab.NewDownloads;

        Assert.True(downloads.Settings.ShowsQuestion);
        Assert.Empty(downloads.Rows);

        await downloads.Settings.TurnOffCommand.ExecuteAsync(null);

        Assert.False(downloads.Settings.ShowsQuestion);
        Assert.False(downloads.Settings.Enabled);
    }

    [AvaloniaFact]
    public async Task On_OnlyFilesThatArriveAfterward_AreOffered()
    {
        Seed("Old Hair-1-1-0.zip", "old.erf", DateTime.UtcNow.AddDays(-1));
        var (window, viewModel) = await _scenario.OpenAsync();
        var downloads = viewModel.ModsTab.NewDownloads;
        await downloads.Settings.TurnOnCommand.ExecuteAsync(null);
        Assert.Empty(downloads.Rows);

        Seed("Blue Hair-1-1-0.zip", "hair.erf");
        Seed("Red Hair-2-1-0.zip", "red.erf");
        Seed("Holiday Photos.zip", "photo.jpg");
        await downloads.LookAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Blue Hair", "Red Hair"], downloads.Rows.Select(r => r.Title).Order());
        Assert.Equal("2 new mods in your downloads", downloads.HomeLine);
        Assert.Empty(viewModel.Library.Mods);
        ModsTabScenario.Show<NewDownloadsPanel>(window);
        Screenshots.Save(window, "39-new-downloads");
    }

    [AvaloniaFact]
    public async Task Add_IsTheUsualImport_AndNotThisOne_SetsItAside_AndNeitherComesBack()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var downloads = viewModel.ModsTab.NewDownloads;
        await downloads.Settings.TurnOnCommand.ExecuteAsync(null);
        Seed("Blue Hair-1-1-0.zip", "hair.erf");
        Seed("Red Hair-2-1-0.zip", "red.erf");
        await downloads.LookAsync(TestContext.Current.CancellationToken);
        var files = _host.FileSystem.AllFiles.Where(f => f.StartsWith(_folder, StringComparison.OrdinalIgnoreCase)).ToList();

        await downloads.AddOneCommand.ExecuteAsync(downloads.Rows.Single(r => r.Title == "Blue Hair"));
        await downloads.NotThisOneCommand.ExecuteAsync(downloads.Rows.Single());

        Assert.Equal("Blue Hair", Assert.Single(viewModel.Library.Mods).Title);
        Assert.Empty(downloads.Rows);
        await downloads.LookAsync(TestContext.Current.CancellationToken);
        Assert.Empty(downloads.Rows);
        Assert.Equal(files, _host.FileSystem.AllFiles.Where(f => f.StartsWith(_folder, StringComparison.OrdinalIgnoreCase)));
    }

    [AvaloniaFact]
    public async Task AFolderThatIsGone_SaysSo_AndOffersChooseFolder()
    {
        var (_, viewModel) = await _scenario.OpenAsync();
        var downloads = viewModel.ModsTab.NewDownloads;

        await downloads.Settings.TurnOnCommand.ExecuteAsync(null);

        Assert.True(downloads.IsError);
        Assert.True(downloads.IsShown);
        Assert.Contains("isn't there", downloads.StatusMessage, StringComparison.Ordinal);

        _host.FileSystem.AddDirectory(MockUnixSupport.Path(@"F:\Mine"));
        _host.Picker.Enqueue(MockUnixSupport.Path(@"F:\Mine"));
        await downloads.Settings.ChooseFolderCommand.ExecuteAsync(null);

        Assert.False(downloads.IsError, downloads.StatusMessage);
    }

    private void Seed(string name, string entry, DateTime? written = null)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
            writer.Write("x");
        }

        var when = written ?? DateTime.UtcNow.AddMinutes(1);
        _host.FileSystem.AddFile(Path.Combine(_folder, name), new MockFileData(buffer.ToArray()) { CreationTime = when, LastWriteTime = when });
    }
}
