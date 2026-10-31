using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Services;
using DAORework.Application.Mods.Library;
using DAORework.Core.Common;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The Mods tab's <i>Library</i> (P3-04): the mods DAORework holds, and importing more from a file, a folder or a drop.
/// Importing only writes the library; nothing is installed into the game's folders.
/// </summary>
public sealed partial class LibraryViewModel : PageViewModel
{
    private static readonly string[] _downloadPatterns = ["*.dazip", "*.zip", "*.7z", "*.rar"];

    private readonly ListLibrary _listLibrary;
    private readonly ImportMod _importMod;
    private readonly CheckKnownDownload _checkKnownDownload;
    private readonly IPathPicker _picker;
    private HashSet<string> _active = new(StringComparer.Ordinal);

    public LibraryViewModel(ListLibrary listLibrary, ImportMod importMod, CheckKnownDownload checkKnownDownload, IPathPicker picker, ILogger<LibraryViewModel> logger)
        : base(logger)
    {
        _listLibrary = listLibrary;
        _importMod = importMod;
        _checkKnownDownload = checkKnownDownload;
        _picker = picker;
    }

    public ObservableCollection<LibraryModRow> Mods { get; } = [];

    public Task LoadAsync(CancellationToken cancellationToken = default) => GuardAsync(RefreshAsync, cancellationToken);

    /// <summary>Marks the mods active in the selected profile's documents folder, now and after every refresh.</summary>
    public void ShowActive(IEnumerable<string> modIds)
    {
        _active = new HashSet<string>(modIds, StringComparer.Ordinal);
        foreach (var row in Mods)
        {
            row.IsActive = _active.Contains(row.Id);
        }
    }

    /// <summary>Imports each path in turn (a drop can hold several) and shows the library afterwards.</summary>
    public Task ImportPathsAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                var imported = new List<string>();
                Error? failure = null;
                foreach (var path in paths)
                {
                    var result = await _importMod.ExecuteAsync(new ImportModRequest(path), token);
                    if (result.TryGetValue(out var import, out var error))
                    {
                        imported.Add(import.AlreadyInLibrary ? $"{import.Entry.Mod.Name} (already in the library)" : import.Entry.Mod.Name);
                    }
                    else
                    {
                        failure = error;
                    }
                }

                await RefreshAsync(token);
                if (failure is not null)
                {
                    ReportError(imported.Count == 0 ? failure : failure with { Message = $"Imported {string.Join(", ", imported)}, but {failure.Message}" });
                    return;
                }

                ReportSuccess(imported.Count == 0 ? string.Empty : $"Imported {string.Join(", ", imported)}. Nothing was installed; the game folders are unchanged.");
            },
            cancellationToken);

    [RelayCommand]
    private async Task ImportFileAsync(CancellationToken cancellationToken)
    {
        // Several downloads can be picked at once, as several can be dropped (P2.5-15); each is imported in turn.
        var paths = await _picker.PickFilesAsync("Import mods", "Mod downloads", _downloadPatterns, cancellationToken);
        if (paths.Count > 0)
        {
            await ImportPathsAsync(paths, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task ImportFolderAsync(CancellationToken cancellationToken)
    {
        var path = await _picker.PickFolderAsync("Import an extracted mod folder", cancellationToken);
        if (path is not null)
        {
            await ImportPathsAsync([path], cancellationToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (!(await _listLibrary.ExecuteAsync(cancellationToken)).TryGetValue(out var entries, out var error))
        {
            ReportError(error);
            return;
        }

        Mods.Clear();
        foreach (var entry in entries)
        {
            var row = LibraryModRow.From(entry, _checkKnownDownload.Execute(entry));
            row.IsActive = _active.Contains(row.Id);
            Mods.Add(row);
        }
    }
}
