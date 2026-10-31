using DAORework.Core.Common;

namespace DAORework.Application.Abstractions;

/// <summary>Computes content hashes without exposing the file system to the caller.</summary>
public interface IFileHasher
{
    /// <summary>Returns the upper-case hex SHA-256 of the file, or a failure when it cannot be read.</summary>
    Task<Result<string>> ComputeSha256Async(string path, CancellationToken cancellationToken = default);
}
