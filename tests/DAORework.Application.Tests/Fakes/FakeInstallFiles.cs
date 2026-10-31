using System.Security.Cryptography;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeInstallFiles : IInstallFiles
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<Error> Problems { get; } = [];

    public Task<Result<byte[]>> ReadAsync(string installDirectory, string relativePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Files.TryGetValue(relativePath, out var bytes)
            ? Result.Success(bytes)
            : Result.Failure<byte[]>(new Error(DiagnosticCodes.FixInstallUnreadable, $"{relativePath} does not exist.")));

    public Task<Result<IReadOnlyDictionary<string, string>>> HashAsync(string installDirectory, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<IReadOnlyDictionary<string, string>>(relativePaths
            .Where(Files.ContainsKey)
            .ToDictionary(p => p, p => Convert.ToHexStringLower(SHA256.HashData(Files[p])), StringComparer.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Error>> CheckWritableAsync(string installDirectory, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Error>>(Problems);
}
