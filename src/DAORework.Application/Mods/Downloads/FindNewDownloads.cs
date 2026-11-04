using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Downloads;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Downloads;

/// <summary>
/// The new mods in the downloads folder (ADR-0034 §2 to §4). Reads names only: nothing is extracted, hashed or scanned
/// until the player adds a file. An archive that isn't a mod is noted as dealt with, so it is opened once; one that can't
/// be listed is left out and looked at again. Asking outright changes nothing saved, so it only reads.
/// </summary>
public sealed class FindNewDownloads
{
    private readonly IDownloadsFolder _folder;
    private readonly INewDownloadsStore _store;
    private readonly IKnownFolders _knownFolders;

    public FindNewDownloads(IDownloadsFolder folder, INewDownloadsStore store, IKnownFolders knownFolders)
    {
        _folder = folder;
        _store = store;
        _knownFolders = knownFolders;
    }

    public async Task<Result<IReadOnlyList<NewDownload>>> ExecuteAsync(FindNewDownloadsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!(await _store.LoadAsync(cancellationToken)).TryGetValue(out var settings, out var error))
        {
            return Result.Failure<IReadOnlyList<NewDownload>>(error);
        }

        if (!settings.Enabled && !request.Asked)
        {
            return Result.Success<IReadOnlyList<NewDownload>>([]);
        }

        var folder = request.Folder ?? settings.Folder ?? _knownFolders.Downloads;
        if (string.IsNullOrWhiteSpace(folder))
        {
            return Result.Failure<IReadOnlyList<NewDownload>>(new Error(DiagnosticCodes.DownloadsFolderMissing, "Windows doesn't report a Downloads folder.", "Choose the folder your browser saves to."));
        }

        if (!_folder.ListFinished(folder).TryGetValue(out var files, out error))
        {
            return Result.Failure<IReadOnlyList<NewDownload>>(error);
        }

        var since = request.Asked ? null : settings.EnabledAtUtc;
        var (offered, notMods) = Sort(files.Where(f => ModDownloadRule.IsNew(f, since, settings.Handled)), cancellationToken);
        if (!request.Asked)
        {
            await KeepHandledAsync(settings, files, notMods, cancellationToken);
        }

        return Result.Success<IReadOnlyList<NewDownload>>(offered);
    }

    private (List<NewDownload> Offered, List<DownloadFile> NotMods) Sort(IEnumerable<DownloadFile> files, CancellationToken cancellationToken)
    {
        var offered = new List<NewDownload>();
        var notMods = new List<DownloadFile>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ModDownloadRule.IsDazip(file.Path))
            {
                offered.Add(Offer(file));
            }
            else if (_folder.ReadEntryNames(file.Path).TryGetValue(out var entries, out _))
            {
                if (ModDownloadRule.LooksLikeMod(entries))
                {
                    offered.Add(Offer(file));
                }
                else
                {
                    notMods.Add(file);
                }
            }
        }

        return (offered, notMods);
    }

    private static NewDownload Offer(DownloadFile file) => new(file, ModIds.NameFromFileName(Path.GetFileNameWithoutExtension(file.FileName)));

    /// <summary>Drops entries for files that are gone, and notes the archives that aren't mods.</summary>
    private async Task KeepHandledAsync(NewDownloadsSettings settings, IReadOnlyList<DownloadFile> files, List<DownloadFile> notMods, CancellationToken cancellationToken)
    {
        var kept = settings.Handled.Where(h => files.Any(h.Is)).Concat(notMods.Select(HandledDownload.From)).ToList();
        if (notMods.Count > 0 || kept.Count != settings.Handled.Count)
        {
            await _store.SaveAsync(settings with { Handled = kept }, cancellationToken);
        }
    }
}
