using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Removes a mod from the library (ADR-0039), to the Recycle Bin where there is one, so putting both back restores it. The
/// <c>mod.json</c> goes first, so a mod is out of the library as soon as any of it is gone. A folder left behind by a failure
/// is an unfinished import's, which the next import replaces.
/// </summary>
public sealed class LibraryRemover : IModRemover
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;
    private readonly IRecycleBin _bin;

    public LibraryRemover(IFileSystem fileSystem, IOptions<DaoReworkOptions> options, IRecycleBin bin)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _bin = bin;
        _root = options.Value.LibraryDirectory;
    }

    public bool GoesToRecycleBin => _bin.IsAvailable;

    public Result Remove(string modId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        var metadata = _fileSystem.Path.Combine(_root, modId + LibraryImporter.MetadataSuffix);
        if (_fileSystem.Path.GetFileName(modId) != modId || !_fileSystem.File.Exists(metadata))
        {
            return Result.Failure(new Error(DiagnosticCodes.ModNotInLibrary, $"{modId} is not in the library."));
        }

        try
        {
            Discard(metadata, () => _fileSystem.File.Delete(metadata));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            return Result.Failure(new Error(DiagnosticCodes.ModRemoveFailed, $"{modId} could not be removed: {exception.Message}", "Nothing of it was removed."));
        }

        try
        {
            var folder = _fileSystem.Path.Combine(_root, modId);
            if (_fileSystem.Directory.Exists(folder))
            {
                Discard(folder, () => _fileSystem.Directory.Delete(folder, recursive: true));
            }

            return Result.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            return Result.Failure(new Error(DiagnosticCodes.ModRemoveFailed, $"{modId} could not be removed: {exception.Message}", "It has left the Library. Its files may still take up space until you add it again."));
        }
    }

    private void Discard(string path, Action delete)
    {
        if (_bin.IsAvailable)
        {
            _bin.Send(path);
        }
        else
        {
            delete();
        }
    }
}
