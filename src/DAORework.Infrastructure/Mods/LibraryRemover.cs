using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Removes a mod from the library (ADR-0039). The <c>mod.json</c> goes first, so a mod is out of the library as soon as
/// any of it is gone. A folder left behind by a failure is an unfinished import's, which the next import replaces.
/// </summary>
public sealed class LibraryRemover : IModRemover
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public LibraryRemover(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.LibraryDirectory;
    }

    public Task<Result> RemoveAsync(string modId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = _fileSystem.Path.Combine(_root, modId + LibraryImporter.MetadataSuffix);
        if (_fileSystem.Path.GetFileName(modId) != modId || !_fileSystem.File.Exists(metadata))
        {
            return Task.FromResult(Result.Failure(new Error(DiagnosticCodes.ModNotInLibrary, $"{modId} is not in the library.")));
        }

        try
        {
            _fileSystem.File.Delete(metadata);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure(new Error(DiagnosticCodes.ModRemoveFailed, $"{modId} could not be removed: {exception.Message}", "Nothing of it was removed.")));
        }

        try
        {
            var folder = _fileSystem.Path.Combine(_root, modId);
            if (_fileSystem.Directory.Exists(folder))
            {
                _fileSystem.Directory.Delete(folder, recursive: true);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Failure(new Error(DiagnosticCodes.ModRemoveFailed, $"{modId} could not be removed: {exception.Message}", "It has left the Library. Its files may still take up space until you add it again.")));
        }
    }
}
