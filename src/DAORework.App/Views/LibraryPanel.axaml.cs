using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using DAORework.App.ViewModels;

namespace DAORework.App.Views;

/// <summary>The Mods tab's <i>Library</i>, with each mod's Activate or Deactivate; files and folders dropped on it are imported (P3-04).</summary>
public sealed partial class LibraryPanel : UserControl
{
    public LibraryPanel()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    /// <summary>The local paths of the files and folders in a drop; items with none, such as a browser link, are left out.</summary>
    private static List<string> LocalPaths(IDataTransfer data) =>
        (data.TryGetFiles() ?? []).Select(item => item.TryGetLocalPath()).OfType<string>().ToList();

    private void OnDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var paths = LocalPaths(e.DataTransfer);
        if (paths.Count > 0 && DataContext is ModsTabViewModel mods)
        {
            await mods.Library.ImportPathsAsync(paths);
        }
    }
}
