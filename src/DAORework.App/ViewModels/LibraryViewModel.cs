using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Services;
using DAORework.Application.Mods.Library;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Downloads;
using DAORework.Core.Mods.Library;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The Mods tab's <i>Library</i> (P3-04): the mods DAORework holds, and importing more from a file, a folder or a drop.
/// Importing only writes the library; nothing is installed into the game's folders.
/// </summary>
public sealed partial class LibraryViewModel : PageViewModel
{
    private static readonly string[] _downloadPatterns = [.. ModDownloadRule.Extensions.Select(e => "*" + e)];

    private readonly ListLibrary _listLibrary;
    private readonly ImportMod _importMod;
    private readonly CheckKnownDownload _checkKnownDownload;
    private readonly IPathPicker _picker;
    private HashSet<string> _active = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, string> _marks = new Dictionary<string, string>();

    public LibraryViewModel(ListLibrary listLibrary, ImportMod importMod, CheckKnownDownload checkKnownDownload, IPathPicker picker, ILogger<LibraryViewModel> logger)
        : base(logger)
    {
        _listLibrary = listLibrary;
        _importMod = importMod;
        _checkKnownDownload = checkKnownDownload;
        _picker = picker;
    }

    public RowList<LibraryModRow> Mods { get; } = [];

    /// <summary>Gets how many mods the library holds more than once (ADR-0039).</summary>
    [ObservableProperty]
    public partial int CopySets { get; private set; }

    /// <summary>Gets how many of those lose nothing by keeping one, which the next step offers. One whose copies differ is the player's to raise.</summary>
    [ObservableProperty]
    public partial int CopiesToClear { get; private set; }

    /// <summary>Gets or sets the look before adding (ADR-0039): it holds back what looks already held, and returns the paths to add now.</summary>
    public Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<string>>>? LookFirstAsync { get; set; }

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

    /// <summary>Shows what each mod's row says about its shared files, now and after every refresh (P2.6-33).</summary>
    public void ShowConflicts(IReadOnlyDictionary<string, string> marks)
    {
        _marks = marks;
        foreach (var row in Mods)
        {
            row.ConflictMark = marks.GetValueOrDefault(row.Id);
        }
    }

    /// <summary>Adds what the player chose: anything that looks already held is asked about first, and the rest is imported.</summary>
    public Task AddPathsAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                var toAdd = LookFirstAsync is { } look ? await look(paths, token) : paths;
                if (toAdd.Count > 0)
                {
                    await ImportPathsAsync(toAdd, token);
                }
            },
            cancellationToken);

    /// <summary>Imports each path in turn (a drop can hold several) and shows the library afterwards.</summary>
    public Task ImportPathsAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                var imported = new List<string>();
                var failed = new List<(string Path, Error Error)>();
                foreach (var path in paths)
                {
                    var result = await ImportOneAsync(path, token);
                    if (result.TryGetValue(out var import, out var error))
                    {
                        imported.Add(import.AlreadyInLibrary ? $"{import.Entry.Mod.Name} (already in the library)" : import.Entry.Mod.Name);
                    }
                    else
                    {
                        failed.Add((path, error));
                    }
                }

                await RefreshAsync(token);
                if (failed.Count > 0)
                {
                    ReportFailed(imported, failed);
                    return;
                }

                ReportSuccess(imported.Count == 0 ? string.Empty : $"Added {string.Join(", ", imported)}. Turn {(imported.Count == 1 ? "it" : "them")} on when you want {(imported.Count == 1 ? "it" : "them")} in your game.");
            },
            cancellationToken);

    private async Task<Result<LibraryImport>> ImportOneAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await _importMod.ExecuteAsync(new ImportModRequest(path), cancellationToken);
        }

        // STANDARDS-EXCEPTION: one download is the boundary of a batch. The failure is logged and named, never lost.
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogUnexpected(exception);
            return Result.Failure<LibraryImport>(new Error(DiagnosticCodes.ModImportFailed, "Something unexpected went wrong while reading it."));
        }
    }

    /// <summary>One download that failed says so in full. Among others, each gets its reason and the rest are still added (P2.6-53).</summary>
    private void ReportFailed(List<string> imported, List<(string Path, Error Error)> failed)
    {
        if (failed.Count == 1 && imported.Count == 0)
        {
            ReportError(failed[0].Error);
            return;
        }

        var added = imported.Count == 0 ? "Nothing was added." : $"Added {Counted.Of(imported.Count, "mod")}.";
        ReportFailure(LeftOut.Outcome(added, [.. failed.Select(f => new LeftOutMod(Path.GetFileName(f.Path), f.Error.Message))]));
    }

    [RelayCommand]
    private async Task ImportFileAsync(CancellationToken cancellationToken)
    {
        // Several downloads can be picked at once, as several can be dropped (P2.5-15); each is imported in turn.
        var paths = await _picker.PickFilesAsync("Add mods", "Mod downloads", _downloadPatterns, cancellationToken);
        if (paths.Count > 0)
        {
            await AddPathsAsync(paths, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task ImportFolderAsync(CancellationToken cancellationToken)
    {
        var path = await _picker.PickFolderAsync("Add an extracted mod folder", cancellationToken);
        if (path is not null)
        {
            await AddPathsAsync([path], cancellationToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (!(await _listLibrary.ExecuteAsync(cancellationToken)).TryGetValue(out var entries, out var error))
        {
            ReportError(error);
            return;
        }

        // Nothing of a mod like a folder of Mac leftovers reaches the game (ADR-0036), so it isn't one; if it is on, it can still be turned off.
        var shown = entries.Where(e => _active.Contains(e.Mod.Id) || !e.Mod.Files.All(f => NonModFiles.Is(f.RelativePath))).ToList();
        var copies = ModCopies.Find(shown, _active);
        var sets = copies.Values.GroupBy(c => c.Group, StringComparer.OrdinalIgnoreCase).ToList();
        CopySets = sets.Count;
        CopiesToClear = sets.Count(s => s.Any(c => c.HoldsAll));
        var onScreen = Mods.ToDictionary(m => m.Id, StringComparer.Ordinal);
        var rows = new List<LibraryModRow>();
        foreach (var entry in shown)
        {
            var read = LibraryModRow.From(entry, _checkKnownDownload.Execute(entry), copies.GetValueOrDefault(entry.Mod.Id));
            var row = onScreen.TryGetValue(read.Id, out var same) && same.ShowsTheSameAs(read) ? same : read;
            row.IsActive = _active.Contains(row.Id);
            row.ConflictMark = _marks.GetValueOrDefault(row.Id);
            row.IsSelected = false;
            row.IsExpanded = false;
            rows.Add(row);
        }

        // Building a row on screen is the slowest thing the window does, so one that says the same stays (P2.6-49). The
        // list is still replaced, since the counts on the cards hear of a change from it, and that alone rebuilds nothing.
        Mods.ReplaceAll(rows);
    }
}
