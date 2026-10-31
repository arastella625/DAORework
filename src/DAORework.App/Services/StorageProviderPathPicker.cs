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
        var files = await OpenFilesAsync(title, filterName, patterns, allowMultiple: false);
        return files.Count == 0 ? null : files[0];
    }

    public Task<IReadOnlyList<string>> PickFilesAsync(string title, string filterName, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default) =>
        OpenFilesAsync(title, filterName, patterns, allowMultiple: true);

    private static async Task<IReadOnlyList<string>> OpenFilesAsync(string title, string filterName, IReadOnlyList<string> patterns, bool allowMultiple)
    {
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = patterns }],
        };
        var files = await RequireProvider().OpenFilePickerAsync(options);
        return [.. files.Select(f => f.TryGetLocalPath()).OfType<string>()];
    }

    private static IStorageProvider RequireProvider()
    {
        var window = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        return TopLevel.GetTopLevel(window)?.StorageProvider
            ?? throw new InvalidOperationException("The main window is not open, so no dialog can be shown.");
    }
}
