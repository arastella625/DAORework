using System.IO.Abstractions;
using System.Security.Cryptography;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>Reads a library file at <c>library/&lt;modId&gt;/&lt;relative path&gt;</c>, where the deployment writer copies it from. Reads only.</summary>
public sealed class LibraryFileReader : ILibraryFileReader
{
    private readonly IFileSystem _fileSystem;
    private readonly string _library;

    public LibraryFileReader(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _library = options.Value.LibraryDirectory;
    }

    public async Task<Result<LibraryFileRead>> ReadAsync(string modId, LibraryModFile file, int startBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!ModPath.IsSafe(modId) || !ModPath.IsSafe(file.RelativePath))
        {
            return Missing(modId, file, "its path isn't one DAORework would write");
        }

        var path = _fileSystem.Path.Combine(_library, modId, file.RelativePath.Replace('/', _fileSystem.Path.DirectorySeparatorChar));
        try
        {
            // One read: the first bytes are kept on the way through the hash.
            await using var stream = _fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var start = new byte[Math.Min(startBytes, stream.Length)];
            await stream.ReadExactlyAsync(start, cancellationToken);
            hash.AppendData(start);
            var buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, read);
            }

            return Result.Success(new LibraryFileRead(path, Convert.ToHexStringLower(hash.GetHashAndReset()), start));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Missing(modId, file, exception.Message);
        }
    }

    private static Result<LibraryFileRead> Missing(string modId, LibraryModFile file, string why) =>
        Result.Failure<LibraryFileRead>(new Error(
            DiagnosticCodes.ModNotInLibrary,
            $"The library copy of {file.RelativePath} from {modId} can't be read: {why}",
            "Add the mod again to put its files back in your Library."));
}
