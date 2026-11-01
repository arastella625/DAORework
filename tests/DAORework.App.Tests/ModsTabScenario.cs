using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using DAORework.App.ViewModels;
using DAORework.App.Views;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>
/// The Mods tab with a profile pointed at the realistic install's documents folder and one small override mod, Hair
/// Pack, ready to import: what the activation and change screens are tested on.
/// </summary>
internal sealed class ModsTabScenario(AppTestHost host)
{
    private static readonly string _download = MockUnixSupport.Path(@"C:\Users\Me\Downloads\Hair Pack.zip");

    public string DocumentsDirectory { get; } = GamePaths.DefaultDocumentsDirectory(host.KnownFolders.MyDocuments);

    /// <summary>A new profile is pointed at the recommended documents folder, as when the user creates one.</summary>
    public async Task<(MainWindow Window, MainWindowViewModel ViewModel)> OpenAsync(bool seedInstall = true)
    {
        if (seedInstall)
        {
            host.SeedRealisticInstall();
        }

        var viewModel = host.Resolve<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        window.Show();
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.Profiles.NewProfileName = "Default";
        await viewModel.Profiles.CreateProfileCommand.ExecuteAsync(null);
        await viewModel.SelectProfileAsync("Default", TestContext.Current.CancellationToken);
        var tabs = window.FindControl<TabControl>("Pages") ?? throw new InvalidOperationException("No tab control named Pages.");
        tabs.SelectedIndex = MainWindowViewModel.ModsPage;
        window.UpdateLayout();
        return (window, viewModel);
    }

    public Task ImportHairPackAsync(MainWindowViewModel viewModel) => ImportAsync(viewModel, _download, ("hair_pack.dds", "DDS hair"));

    /// <summary>Imports a zip of <paramref name="files"/> saved at <paramref name="path"/>, through the Library.</summary>
    public async Task ImportAsync(MainWindowViewModel viewModel, string path, params (string Name, string Content)[] files)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        host.FileSystem.AddFile(path, new MockFileData(buffer.ToArray()));
        await viewModel.Library.ImportPathsAsync([path], TestContext.Current.CancellationToken);
        Assert.False(viewModel.Library.IsError, viewModel.Library.StatusMessage);
    }

    public Task ActivateHairPackAsync(MainWindowViewModel viewModel) => ActivateAsync(viewModel, "Hair Pack");

    /// <summary>Activates the library mod named <paramref name="title"/> and applies the plan.</summary>
    public async Task ActivateAsync(MainWindowViewModel viewModel, string title)
    {
        await viewModel.ModsTab.Activation.ActivateCommand.ExecuteAsync(viewModel.Library.Mods.Single(m => m.Title == title));
        await viewModel.ModsTab.Activation.ApplyCommand.ExecuteAsync(null);
        Assert.False(viewModel.ModsTab.Activation.IsError, viewModel.ModsTab.Activation.StatusMessage);
    }

    /// <summary>The content of the deployed copy of <paramref name="fileName"/> under the documents folder's override.</summary>
    public string DeployedContent(string fileName)
    {
        var overrideFolder = GamePaths.OverrideDirectory(DocumentsDirectory);
        var deployed = host.FileSystem.AllFiles.Single(f => f.StartsWith(overrideFolder, StringComparison.Ordinal) && Path.GetFileName(f) == fileName);
        return host.FileSystem.File.ReadAllText(deployed);
    }

    /// <summary>The hair texture deployed anywhere under the documents folder's override, never the library's copy.</summary>
    public List<string> DeployedHair()
    {
        var overrideFolder = GamePaths.OverrideDirectory(DocumentsDirectory);
        return host.FileSystem.AllFiles.Where(f => f.StartsWith(overrideFolder, StringComparison.Ordinal) && f.EndsWith("hair_pack.dds", StringComparison.Ordinal)).ToList();
    }

    /// <summary>Scrolls the Mods tab so <typeparamref name="TPanel"/>'s heading is at the top of a screenshot.</summary>
    public static void Show<TPanel>(MainWindow window)
        where TPanel : Control
    {
        // The Mods tab shows one section at a time (P2.5-17): select the panel's section, then scroll to the panel.
        window.UpdateLayout();
        var panel = window.GetLogicalDescendants().OfType<TPanel>().Distinct().Single();
        var section = panel.FindLogicalAncestorOfType<TabItem>() ?? throw new InvalidOperationException("The panel is not in a section.");
        section.IsSelected = true;
        window.UpdateLayout();
        var scroller = panel.FindAncestorOfType<ScrollViewer>() ?? throw new InvalidOperationException("The panel is not in a ScrollViewer.");
        scroller.Offset = new Avalonia.Vector(0, panel.Bounds.Top);
        window.UpdateLayout();
    }
}
