using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Mods;

namespace DAORework.Infrastructure.Fixes;

/// <summary>
/// Reads the install folder for managed fixes (ADR-0013) and checks it can be written without administrator rights:
/// a file already in each folder in play is opened for writing and closed again, and each file in play is opened for
/// exclusive write, which fails while the game has it open. Nothing is written or created, so a check cut short by a
/// crash leaves nothing behind (ADR-0014).
/// </summary>
public sealed class InstallFolderFiles : IInstallFiles
{
    private readonly IFileSystem _fileSystem;

    public InstallFolderFiles(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<byte[]>> ReadAsync(string installDirectory, string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = DocumentsPaths.Resolve(_fileSystem, installDirectory, relativePath);
            return _fileSystem.File.Exists(path)
                ? Result.Success(await _fileSystem.File.ReadAllBytesAsync(path, cancellationToken))
                : Unreadable<byte[]>($"{relativePath} is not in {installDirectory}.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Unreadable<byte[]>($"{relativePath} could not be read: {exception.Message}");
        }
    }

    public async Task<Result<IReadOnlyDictionary<string, string>>> HashAsync(string installDirectory, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var relative in relativePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var path = DocumentsPaths.Resolve(_fileSystem, installDirectory, relative);
                if (_fileSystem.File.Exists(path))
                {
                    hashes[relative] = await FileHashes.ComputeAsync(_fileSystem, path, cancellationToken);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Unreadable<IReadOnlyDictionary<string, string>>($"The install folder could not be read: {exception.Message}");
        }

        return Result.Success<IReadOnlyDictionary<string, string>>(hashes);
    }

    public Task<IReadOnlyList<Error>> CheckWritableAsync(string installDirectory, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default)
    {
        var paths = relativePaths.Select(p => DocumentsPaths.Resolve(_fileSystem, installDirectory, p)).ToList();
        var problems = paths
            .Select(p => _fileSystem.Path.GetDirectoryName(p)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(_fileSystem.Directory.Exists)
            .Select(CheckFolder)
            .OfType<Error>()
            .ToList();

        // A folder that cannot be written makes every file in it fail the same way, so the files are checked only when the folders pass.
        if (problems.Count == 0)
        {
            problems.AddRange(paths.Where(_fileSystem.File.Exists).Select(CheckFile).OfType<Error>());
        }

        return Task.FromResult<IReadOnlyList<Error>>(problems);
    }

    private static Result<T> Unreadable<T>(string message) => Result.Failure<T>(new Error(DiagnosticCodes.FixInstallUnreadable, message));

    private static Error NotWritable(string path) => new(
        DiagnosticCodes.FixInstallNotWritable,
        $"DAORework cannot write to {path} without administrator rights.",
        "Run DAORework as administrator, or move the game to a Steam library outside Program Files. DAORework never asks for administrator rights itself.");

    /// <summary>
    /// Opens a file already in the folder for writing, shared, and closes it without writing: under Program Files a
    /// user without administrator rights may neither change nor add files. A folder with no file in it is not checked.
    /// </summary>
    private Error? CheckFolder(string folder)
    {
        var existing = _fileSystem.Directory.EnumerateFiles(folder).FirstOrDefault();
        if (existing is null)
        {
            return null;
        }

        try
        {
            using (_fileSystem.FileStream.New(existing, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
            {
            }

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return NotWritable(folder);
        }
        catch (IOException)
        {
            // Held open by another program: whether the files in play can be changed is checked one by one next.
            return null;
        }
    }

    private Error? CheckFile(string path)
    {
        try
        {
            using (_fileSystem.FileStream.New(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return NotWritable(path);
        }
        catch (IOException)
        {
            return new Error(DiagnosticCodes.FixFileInUse, $"{path} is in use, most likely by the running game.", "Close the game and try again.");
        }
    }
}
