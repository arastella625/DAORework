using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Planning;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Moves one hand-installed file to where adoption puts it (ADR-0015, <see cref="PlanAction.Move"/>): a rename inside
/// the documents folder, so there is no partial copy to recover from. Safe to repeat: the file already at the target
/// with its hash and gone from where it was is success; any other state is refused and nothing is touched.
/// </summary>
internal sealed class DocumentsFileMover(IFileSystem fileSystem)
{
    public async Task<Result> MoveAsync(string documentsDirectory, PlanOperation operation, CancellationToken cancellationToken)
    {
        if (operation.MovedFrom is null || operation.ExpectedSha256 is null)
        {
            return Failed($"The move to {operation.TargetPath} does not say where from or what it moves.");
        }

        var from = DocumentsPaths.Resolve(fileSystem, documentsDirectory, operation.MovedFrom);
        var to = DocumentsPaths.Resolve(fileSystem, documentsDirectory, operation.TargetPath);
        var atSource = await HashAsync(from, cancellationToken);
        var atTarget = await HashAsync(to, cancellationToken);
        if (atSource is null && atTarget is not null && FileHashes.Same(atTarget, operation.ExpectedSha256))
        {
            return Result.Success();
        }

        if (atTarget is not null)
        {
            return Failed($"{operation.TargetPath} already exists; {operation.MovedFrom} is not moved onto it.");
        }

        if (atSource is null || !FileHashes.Same(atSource, operation.ExpectedSha256))
        {
            return Failed(atSource is null
                ? $"{operation.MovedFrom} is gone; it was not moved."
                : $"{operation.MovedFrom} changed after the plan was made; it is not moved.");
        }

        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(to)!);
        fileSystem.File.Move(from, to, overwrite: false);
        EmptyFolders.PruneUpFrom(fileSystem, documentsDirectory, fileSystem.Path.GetDirectoryName(from)!);
        return Result.Success();
    }

    private static Result Failed(string message) => Result.Failure(new Error(DiagnosticCodes.ModApplyFailed, message));

    private async Task<string?> HashAsync(string path, CancellationToken cancellationToken) =>
        fileSystem.File.Exists(path) ? await FileHashes.ComputeAsync(fileSystem, path, cancellationToken) : null;
}
