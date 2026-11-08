using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Puts one backup back (P2-04). A file that existed is copied back through a temporary file next to it and verified
/// by hash; a path that was empty is emptied again, but only while it holds what the plan wrote there; a moved file is
/// moved back (<see cref="MovedFileRestorer"/>, ADR-0015). A file the Library holds comes from there (ADR-0045), and every one
/// of those is found before anything changes, so a copy that has gone stops the restore with the folder as it was.
/// </summary>
internal sealed class BackupRestorer(IFileSystem fileSystem, DeploymentBackup backup, string folder, MovedFileRestorer moves, LibraryCopies copies)
{
    private const string TempSuffix = ".daorework-restore";

    public async Task<Result<RestoreReport>> RestoreAsync(CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        foreach (var entry in backup.Entries.Where(e => e is { BeforeSha256: not null, StoredAs: null }))
        {
            if (await copies.FindAsync(entry.BeforeSha256!, cancellationToken) is null)
            {
                missing.Add(fileSystem.Path.GetFileName(entry.TargetPath));
            }
        }

        return missing.Count == 0 ? await PutEachBackAsync(cancellationToken) : Result.Failure<RestoreReport>(new Error(
            DiagnosticCodes.ModUndoNeedsRemovedMod,
            missing.Count == 1
                ? $"This can't be put back yet: {missing[0]} is gone from your Library or damaged there."
                : $"This can't be put back yet: {missing.Count} of its files, such as {missing[0]}, are gone from your Library or damaged there.",
            "If its mod is still in your Library, remove it first, since adding a download you already have repairs nothing. Then add the same download again and try this again. Nothing was changed."));
    }

    private async Task<Result<RestoreReport>> PutEachBackAsync(CancellationToken cancellationToken)
    {
        int restored = 0, removed = 0;
        var leftInPlace = new List<string>();
        foreach (var entry in backup.Entries)
        {
            var path = DocumentsPaths.Resolve(fileSystem, backup.DocumentsDirectory, entry.TargetPath);
            DeploymentTempFiles.RemoveStale(fileSystem, path);
            if (entry.MovedFrom is { } movedFrom)
            {
                if (!(await moves.RestoreAsync(entry, movedFrom, cancellationToken)).TryGetValue(out var moved, out var error))
                {
                    return Result.Failure<RestoreReport>(error);
                }

                restored += moved.Restored ? 1 : 0;
                if (moved.LeftTargetInPlace)
                {
                    leftInPlace.Add(entry.TargetPath);
                }

                continue;
            }

            if (entry.BeforeSha256 is null)
            {
                switch (await EmptyAsync(path, entry, cancellationToken))
                {
                    case true:
                        removed++;
                        break;
                    case false:
                        leftInPlace.Add(entry.TargetPath);
                        break;
                }

                continue;
            }

            if (!await PutBackAsync(path, entry, cancellationToken))
            {
                return Result.Failure<RestoreReport>(new Error(
                    DiagnosticCodes.ModRestoreFailed,
                    $"{entry.TargetPath} does not match its backup after restoring it.",
                    "The backup is kept; roll back again once nothing else is using the file."));
            }

            restored++;
        }

        return Result.Success(new RestoreReport(restored, removed, leftInPlace));
    }

    /// <summary>Removes what the plan created. Null when nothing is there, false when it is someone else's file now.</summary>
    private async Task<bool?> EmptyAsync(string path, BackupEntry entry, CancellationToken cancellationToken)
    {
        if (!fileSystem.File.Exists(path))
        {
            return null;
        }

        if (entry.AfterSha256 is not null && !FileHashes.Same(await FileHashes.ComputeAsync(fileSystem, path, cancellationToken), entry.AfterSha256))
        {
            return false;
        }

        fileSystem.File.Delete(path);
        EmptyFolders.PruneUpFrom(fileSystem, backup.DocumentsDirectory, fileSystem.Path.GetDirectoryName(path)!);
        return true;
    }

    private async Task<bool> PutBackAsync(string path, BackupEntry entry, CancellationToken cancellationToken)
    {
        var temp = path + TempSuffix;
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        if (fileSystem.File.Exists(temp))
        {
            fileSystem.File.Delete(temp);
        }

        var source = entry.StoredAs is { } stored ? fileSystem.Path.Combine(folder, stored) : await copies.FindAsync(entry.BeforeSha256!, cancellationToken);
        if (!FileHashes.Same(await FileHashes.CopyAsync(fileSystem, source!, temp, cancellationToken), entry.BeforeSha256!))
        {
            fileSystem.File.Delete(temp);
            return false;
        }

        fileSystem.File.Move(temp, path, overwrite: true);
        return FileHashes.Same(await FileHashes.ComputeAsync(fileSystem, path, cancellationToken), entry.BeforeSha256!);
    }
}
