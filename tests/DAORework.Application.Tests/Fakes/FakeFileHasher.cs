using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Hashes to "HASH:" plus the file name; fails for any path in <see cref="UnreadablePaths"/>.</summary>
internal sealed class FakeFileHasher : IFileHasher
{
    public HashSet<string> UnreadablePaths { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> RequestedPaths { get; } = [];

    public Task<Result<string>> ComputeSha256Async(string path, CancellationToken cancellationToken = default)
    {
        RequestedPaths.Add(path);
        var result = UnreadablePaths.Contains(path)
            ? Result.Failure<string>(new Error(DiagnosticCodes.FileReadFailed, $"unreadable: {path}"))
            : Result.Success("HASH:" + Path.GetFileName(path));
        return Task.FromResult(result);
    }
}
