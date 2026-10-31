namespace DAORework.App.Services;

/// <summary>Opens the OS folder or file dialog. A port so the view models can be tested without a window.</summary>
public interface IPathPicker
{
    /// <returns>The chosen folder, or null when the user cancelled.</returns>
    Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken = default);

    /// <returns>The chosen file, or null when the user cancelled.</returns>
    Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default);

    /// <returns>The chosen files, in the order the dialog gives them, or none when the user cancelled (P2.5-15).</returns>
    Task<IReadOnlyList<string>> PickFilesAsync(string title, string filterName, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default);
}
