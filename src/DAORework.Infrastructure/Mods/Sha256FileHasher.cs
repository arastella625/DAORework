using System.IO.Abstractions;
using System.Security.Cryptography;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Infrastructure.Mods;

public sealed class Sha256FileHasher : IFileHasher
{
    private readonly IFileSystem _fileSystem;

    public Sha256FileHasher(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<string>> ComputeSha256Async(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = _fileSystem.File.OpenRead(path);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Result.Success(Convert.ToHexString(hash));
        }
        catch (IOException exception)
        {
            return ReadFailed(path, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            return ReadFailed(path, exception);
        }
    }

    private static Result<string> ReadFailed(string path, Exception exception) =>
        Result.Failure<string>(new Error(DiagnosticCodes.FileReadFailed, $"The file could not be read: {path}. {exception.Message}"));
}
