using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Planning;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Copies library files into the documents folder, replaces and deletes them (ADR-0012, ROADMAP P2-05), and moves the
/// files an adoption takes over (ADR-0015, <see cref="DocumentsFileMover"/>). Each step
/// checks the target first: already in its end state is success without writing, in its planned before-state is
/// changed, anything else is refused. New content goes through a temporary file next to the target and is verified
/// against the library hash before it is moved into place, so a crash leaves the target whole.
/// </summary>
public sealed class DocumentsDeploymentWriter : IDeploymentWriter
{
    private readonly IFileSystem _fileSystem;
    private readonly string _library;
    private readonly AddInsListEditor _addIns;
    private readonly DocumentsFileMover _mover;

    public DocumentsDeploymentWriter(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _library = options.Value.LibraryDirectory;
        _addIns = new AddInsListEditor(fileSystem, _library);
        _mover = new DocumentsFileMover(fileSystem);
    }

    public async Task<Result> ApplyAsync(string documentsDirectory, PlanOperation operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var path = DocumentsPaths.Resolve(_fileSystem, documentsDirectory, operation.TargetPath);
        try
        {
            if (operation.Action == PlanAction.Move)
            {
                return await _mover.MoveAsync(documentsDirectory, operation, cancellationToken);
            }

            var current = _fileSystem.File.Exists(path) ? await FileHashes.ComputeAsync(_fileSystem, path, cancellationToken) : null;
            return operation.Action == PlanAction.Delete
                ? Delete(documentsDirectory, path, operation, current)
                : await WriteAsync(path, operation, current, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed($"{operation.TargetPath}: {exception.Message}");
        }
        finally
        {
            DeploymentTempFiles.RemoveStale(_fileSystem, path);
        }
    }

    public Task<Result> EditAddInsListAsync(string documentsDirectory, IReadOnlyList<AddInsListChange> changes, CancellationToken cancellationToken = default) =>
        _addIns.EditAsync(DocumentsPaths.Resolve(_fileSystem, documentsDirectory, "Settings/AddIns.xml"), changes, cancellationToken);

    private static Result Failed(string message) => Result.Failure(new Error(DiagnosticCodes.ModApplyFailed, message));

    private static bool Same(string? a, string? b) => a is not null && b is not null && FileHashes.Same(a, b);

    private Result Delete(string documentsDirectory, string path, PlanOperation operation, string? current)
    {
        if (current is null)
        {
            return Result.Success();
        }

        if (!Same(current, operation.ExpectedSha256))
        {
            return Failed($"{operation.TargetPath} changed after the plan was made; it is not deleted.");
        }

        _fileSystem.File.Delete(path);
        EmptyFolders.PruneUpFrom(_fileSystem, documentsDirectory, _fileSystem.Path.GetDirectoryName(path)!);
        return Result.Success();
    }

    private async Task<Result> WriteAsync(string path, PlanOperation operation, string? current, CancellationToken cancellationToken)
    {
        var source = operation.Source!;
        if (Same(current, source.Source.Sha256))
        {
            return Result.Success();
        }

        var replacing = operation.Action == PlanAction.Replace;
        if (replacing ? !Same(current, operation.ExpectedSha256) : current is not null)
        {
            return Failed($"{operation.TargetPath} is not what the plan found there; it is left alone.");
        }

        var libraryFile = _fileSystem.Path.Combine(_library, source.ModId, source.Source.RelativePath.Replace('/', _fileSystem.Path.DirectorySeparatorChar));
        if (!_fileSystem.File.Exists(libraryFile))
        {
            return Failed($"The library copy of {source.Source.RelativePath} from {source.ModId} is missing.");
        }

        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(path)!);
        DeploymentTempFiles.RemoveStale(_fileSystem, path);
        var temp = path + DeploymentTempFiles.Suffix;
        if (!FileHashes.Same(await FileHashes.CopyAsync(_fileSystem, libraryFile, temp, cancellationToken), source.Source.Sha256))
        {
            return Failed($"The library copy of {source.Source.RelativePath} from {source.ModId} does not match its recorded hash.");
        }

        _fileSystem.File.Move(temp, path, overwrite: replacing);
        return Result.Success();
    }
}
