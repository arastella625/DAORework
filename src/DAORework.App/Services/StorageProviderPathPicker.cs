using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace DAORework.App.Services;

/// <summary>The <see cref="IPathPicker"/> over Avalonia's storage provider, anchored to the main window.</summary>
public sealed class StorageProviderPathPicker : IPathPicker
{
    public async Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken = default)
    {
        var provider = RequireProvider();
        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    public async Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default)
    {
        var provider = RequireProvider();
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = patterns }],
        };
        var files = await provider.OpenFilePickerAsync(options);
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    private static IStorageProvider RequireProvider()
    {
        var window = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        return TopLevel.GetTopLevel(window)?.StorageProvider
            ?? throw new InvalidOperationException("The main window is not open, so no dialog can be shown.");
    }
}
