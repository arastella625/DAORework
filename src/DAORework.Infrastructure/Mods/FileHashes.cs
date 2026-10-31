using System.IO.Abstractions;
using System.Security.Cryptography;

namespace DAORework.Infrastructure.Mods;

/// <summary>SHA-256 over <see cref="IFileSystem"/>, lower-case hex, and a copy that hashes what it reads on the way.</summary>
internal static class FileHashes
{
    public static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public static async Task<string> ComputeAsync(IFileSystem fileSystem, string path, CancellationToken cancellationToken)
    {
        await using var stream = fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
    }

    /// <summary>Writes <paramref name="input"/> to a new file at <paramref name="target"/>; returns its hash and length.</summary>
    public static async Task<(string Sha256, long Length)> WriteAsync(IFileSystem fileSystem, Stream input, string target, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var output = fileSystem.FileStream.New(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920];
        long length = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            length += read;
        }

        return (Convert.ToHexStringLower(hash.GetHashAndReset()), length);
    }

    /// <summary>Copies <paramref name="source"/> to a new file at <paramref name="target"/> and returns the source's hash.</summary>
    public static async Task<string> CopyAsync(IFileSystem fileSystem, string source, string target, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var input = fileSystem.FileStream.New(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var output = fileSystem.FileStream.New(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
