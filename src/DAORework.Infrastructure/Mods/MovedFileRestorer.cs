using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Undoes one move an adoption made (ADR-0015): the file goes back where it was. When it is no longer where the move
/// put it, its copy comes from the library, found by its SHA-256. Safe to repeat. A different file where the moved one
/// was is never overwritten, and a different file where it was moved to is left in place and reported.
/// </summary>
internal sealed class MovedFileRestorer(
    IFileSystem fileSystem,
    string documentsDirectory,
    Func<string, CancellationToken, Task<string?>> findInLibrary)
{
    private const string TempSuffix = ".daorework-restore";

    /// <returns>Whether a file was put back, and whether a different file at the target was left in place.</returns>
    public async Task<Result<(bool Restored, bool LeftTargetInPlace)>> RestoreAsync(BackupEntry entry, string movedFrom, CancellationToken cancellationToken)
    {
        var sha = entry.AfterSha256 ?? throw new InvalidDataException($"The backup's move to {entry.TargetPath} has no hash.");
        var from = DocumentsPaths.Resolve(fileSystem, documentsDirectory, movedFrom);
        var to = DocumentsPaths.Resolve(fileSystem, documentsDirectory, entry.TargetPath);
        var atFrom = await HashAsync(from, cancellationToken);
        var atTo = await HashAsync(to, cancellationToken);
        var toIsOurs = atTo is not null && FileHashes.Same(atTo, sha);
        if (atFrom is not null)
        {
            if (!FileHashes.Same(atFrom, sha))
            {
                return Failed($"{entry.MovedFrom} holds a different file now; it is not overwritten.");
            }

            if (toIsOurs)
            {
                Remove(to);
            }

            return Result.Success((false, atTo is not null && !toIsOurs));
        }

        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(from)!);
        if (toIsOurs)
        {
            fileSystem.File.Move(to, from, overwrite: false);
            EmptyFolders.PruneUpFrom(fileSystem, documentsDirectory, fileSystem.Path.GetDirectoryName(to)!);
            return Result.Success((true, false));
        }

        return await FromLibraryAsync(movedFrom, entry.TargetPath, from, sha, cancellationToken) is { } problem
            ? Failed(problem)
            : Result.Success((true, atTo is not null));
    }

    private static Result<(bool Restored, bool LeftTargetInPlace)> Failed(string message) =>
        Result.Failure<(bool Restored, bool LeftTargetInPlace)>(new Error(DiagnosticCodes.ModRestoreFailed, message, "The backup is kept; nothing else was changed for this file."));

    /// <summary>Copies the library's copy back through a temporary file. Returns why it could not, or null.</summary>
    private async Task<string?> FromLibraryAsync(string movedFrom, string movedTo, string from, string sha, CancellationToken cancellationToken)
    {
        if (await findInLibrary(sha, cancellationToken) is not { } copy)
        {
            return $"{movedFrom} is no longer at {movedTo}, and the library has no copy with its hash.";
        }

        // A restore killed during this copy leaves its temporary file behind; it is only ever a partial copy.
        var temp = from + TempSuffix;
        if (fileSystem.File.Exists(temp))
        {
            fileSystem.File.Delete(temp);
        }

        if (!FileHashes.Same(await FileHashes.CopyAsync(fileSystem, copy, temp, cancellationToken), sha))
        {
            fileSystem.File.Delete(temp);
            return $"The library's copy of {movedFrom} does not match its hash.";
        }

        fileSystem.File.Move(temp, from, overwrite: false);
        return null;
    }

    private void Remove(string path)
    {
        fileSystem.File.Delete(path);
        EmptyFolders.PruneUpFrom(fileSystem, documentsDirectory, fileSystem.Path.GetDirectoryName(path)!);
    }

    private async Task<string?> HashAsync(string path, CancellationToken cancellationToken) =>
        fileSystem.File.Exists(path) ? await FileHashes.ComputeAsync(fileSystem, path, cancellationToken) : null;
}
