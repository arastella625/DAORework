using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>
/// Reads the game's install folder for managed fixes (ADR-0013), with paths relative to it and <c>/</c> separators.
/// It never writes there: the one write check creates and deletes a file of DAORework's own.
/// </summary>
public interface IInstallFiles
{
    Task<Result<byte[]>> ReadAsync(string installDirectory, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>The SHA-256 of each path that exists; a path with no file is left out.</summary>
    Task<Result<IReadOnlyDictionary<string, string>>> HashAsync(string installDirectory, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Why the paths cannot be changed now, if they cannot: <c>DAO_FIX_INSTALL_NOT_WRITABLE</c> when DAORework has no
    /// write access to their folder, <c>DAO_FIX_FILE_IN_USE</c> for a file another program holds open. Empty when all is well.
    /// </summary>
    Task<IReadOnlyList<Error>> CheckWritableAsync(string installDirectory, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default);
}
