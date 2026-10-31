using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Diagnostics;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>The Mods tab's <i>Library</i> (P3-04), over the real import, the antivirus port and the catalog that ships.</summary>
public sealed class LibraryScreenTests : IDisposable
{
    private static readonly string _downloads = MockUnixSupport.Path(@"C:\Users\Me\Downloads");

    private readonly AppTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [AvaloniaFact]
    public async Task Library_Empty_SaysWhatImportingDoes()
    {
        var (window, viewModel) = await OpenModsTabAsync();

        Assert.Empty(viewModel.Library.Mods);
        Assert.True(Panel(window).FindControl<TextBlock>("LibraryEmpty")!.IsVisible);
    }

    [AvaloniaFact]
    public async Task ImportAMod_FromThePicker_ListsItWithItsSafetyNotes()
    {
        var dazip = Path.Combine(_downloads, "Extra Dog Slot.dazip");
        _host.FileSystem.AddFile(dazip, new MockFileData(Archive(
            ("Manifest.xml", "<Manifest><AddInsList><AddInItem UID=\"Extra_Dog_Slot\" Name=\"Extra Dog Slot\" /></AddInsList></Manifest>"),
            ("Contents/addins/Extra_Dog_Slot/core/dog.gda", "dog"))));
        var (window, viewModel) = await OpenModsTabAsync();
        _host.Picker.Enqueue(dazip);

        await viewModel.Library.ImportFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.Library.IsError, viewModel.Library.StatusMessage);
        Assert.Equal("Imported Extra Dog Slot. Nothing was installed; the game folders are unchanged.", viewModel.Library.StatusMessage);
        var row = Assert.Single(viewModel.Library.Mods);
        Assert.Equal("Extra Dog Slot", row.Title);
        Assert.Contains("add-in Extra_Dog_Slot · 2 file(s)", row.Details, StringComparison.Ordinal);
        Assert.Contains("Not scanned: The Windows antimalware scan (AMSI) is only on Windows.", row.Notes);
        Assert.Equal(
            $"Extra Dog Slot.dazip differs from the known downloads of Extra Dog Slot. Unless it is a newer version, get it from https://www.nexusmods.com/dragonage/mods/664 ({DiagnosticCodes.ModSafetyDownloadDiffers})",
            row.Warning);
        Assert.Equal("Import mods", Assert.Single(_host.Picker.Titles));
        Assert.False(Panel(window).FindControl<TextBlock>("LibraryEmpty")!.IsVisible);
        ShowPanel(window);
        Screenshots.Save(window, "13-library");
    }

    [AvaloniaFact]
    public async Task ImportMods_SeveralPickedAtOnce_ImportsEachWithOneSummary()
    {
        var eyes = Path.Combine(_downloads, "Eyes.zip");
        var nails = Path.Combine(_downloads, "Nails.zip");
        _host.FileSystem.AddFile(eyes, new MockFileData(Archive(("eyes.dds", "eyes"))));
        _host.FileSystem.AddFile(nails, new MockFileData(Archive(("nails.dds", "nails"))));
        var (_, viewModel) = await OpenModsTabAsync();
        _host.Picker.Enqueue(eyes);
        _host.Picker.Enqueue(nails);

        await viewModel.Library.ImportFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.Library.IsError, viewModel.Library.StatusMessage);
        Assert.Equal("Imported Eyes, Nails. Nothing was installed; the game folders are unchanged.", viewModel.Library.StatusMessage);
        Assert.Equal(["Eyes", "Nails"], viewModel.Library.Mods.Select(m => m.Title).Order());
        Assert.Equal("Import mods", Assert.Single(_host.Picker.Titles));
    }

    [AvaloniaFact]
    public async Task ImportMods_Cancelled_ImportsNothing()
    {
        var (_, viewModel) = await OpenModsTabAsync();

        await viewModel.Library.ImportFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.Library.IsError, viewModel.Library.StatusMessage);
        Assert.Empty(viewModel.Library.Mods);
    }

    [AvaloniaFact]
    public async Task Drop_SeveralFiles_ImportsEachAndReportsTheOneThatFailed()
    {
        var pack = Path.Combine(_downloads, "Hair Pack.zip");
        var broken = Path.Combine(_downloads, "Broken.rar");
        _host.FileSystem.AddFile(pack, new MockFileData(Archive(("hair.dds", "DDS"), ("tools/setup.exe", "MZ"))));
        _host.FileSystem.AddFile(broken, new MockFileData("not a rar"u8.ToArray()));
        var (_, viewModel) = await OpenModsTabAsync();

        await viewModel.Library.ImportPathsAsync([pack, broken], TestContext.Current.CancellationToken);

        Assert.True(viewModel.Library.IsError);
        Assert.StartsWith("Imported Hair Pack, but ", viewModel.Library.StatusMessage, StringComparison.Ordinal);
        Assert.EndsWith($"({DiagnosticCodes.ModImportInvalid})", viewModel.Library.StatusMessage, StringComparison.Ordinal);
        var row = Assert.Single(viewModel.Library.Mods);
        Assert.Contains("It holds 1 program file(s) (tools/setup.exe). They stay in the library and are never installed into the game's folders.", row.Notes);
        Assert.Null(row.Warning);
    }

    [AvaloniaFact]
    public async Task ImportAFolder_Cancelled_ChangesNothing()
    {
        var (_, viewModel) = await OpenModsTabAsync();

        await viewModel.Library.ImportFolderCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Library.Mods);
        Assert.Equal(string.Empty, viewModel.Library.StatusMessage);
        Assert.Equal("Import an extracted mod folder", Assert.Single(_host.Picker.Titles));
    }

    private static byte[] Archive(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }

    private static LibraryPanel Panel(MainWindow window) => window.GetVisualDescendants().OfType<LibraryPanel>().Single();

    /// <summary>Scroll so the panel's heading is at the top of the screenshot.</summary>
    private static void ShowPanel(MainWindow window)
    {
        window.UpdateLayout();
        var panel = Panel(window);
        var scroller = panel.FindAncestorOfType<ScrollViewer>() ?? throw new InvalidOperationException("The panel is not in a ScrollViewer.");
        scroller.Offset = new Avalonia.Vector(0, panel.Bounds.Top);
        window.UpdateLayout();
    }

    private async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenModsTabAsync()
    {
        _host.SeedRealisticInstall();
        var viewModel = _host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        viewModel.Profiles.NewProfileName = "Default";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);

        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = 2;
        window.UpdateLayout();
        return (window, viewModel);
    }
}
