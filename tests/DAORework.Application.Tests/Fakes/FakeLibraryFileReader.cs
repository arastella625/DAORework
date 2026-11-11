using System.Security.Cryptography;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Library files by "modId/relative path", read as they are now.</summary>
internal sealed class FakeLibraryFileReader : ILibraryFileReader
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<Result<LibraryFileRead>> ReadAsync(string modId, LibraryModFile file, int startBytes, CancellationToken cancellationToken = default)
    {
        var key = $"{modId}/{file.RelativePath}";
        return Task.FromResult(Files.TryGetValue(key, out var bytes)
            ? Result.Success(new LibraryFileRead($"library/{key}", Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes[..Math.Min(startBytes, bytes.Length)]))
            : Result.Failure<LibraryFileRead>(new Error(DiagnosticCodes.ModNotInLibrary, $"{key} is missing.")));
    }
}
