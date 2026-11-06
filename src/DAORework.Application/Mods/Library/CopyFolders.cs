using DAORework.Application.Abstractions;
using DAORework.Application.Documents;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Library;

/// <summary>What is on in every documents folder the profiles name (ADR-0039). A record that can't be read stops the caller: nothing is removed on a guess.</summary>
public sealed class CopyFolders
{
    private readonly ProfileDocumentsFolders _folders;
    private readonly IDeploymentRecordStore _records;
    private readonly IApplyJournalStore _journals;

    public CopyFolders(ProfileDocumentsFolders folders, IDeploymentRecordStore records, IApplyJournalStore journals)
    {
        _folders = folders;
        _records = records;
        _journals = journals;
    }

    /// <param name="selected">The folder in use, read even if no profile names it yet.</param>
    /// <param name="cancellationToken">Stops the reading.</param>
    public async Task<Result<IReadOnlyList<FolderMods>>> ReadAsync(string? selected, CancellationToken cancellationToken = default)
    {
        var paths = (await _folders.ReadAsync(cancellationToken)).Append(selected).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase);
        var folders = new List<FolderMods>();
        foreach (var path in paths)
        {
            if (!(await _records.LoadAsync(path, cancellationToken)).TryGetValue(out var record, out var error))
            {
                return Result.Failure<IReadOnlyList<FolderMods>>(error);
            }

            folders.Add(new FolderMods(path, record.ActiveModIds, await _journals.LoadAsync(path, cancellationToken) is not null));
        }

        return Result.Success<IReadOnlyList<FolderMods>>(folders);
    }
}
