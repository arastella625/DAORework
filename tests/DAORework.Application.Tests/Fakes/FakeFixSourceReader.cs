using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeFixSourceReader : IFixSourceReader
{
    public Dictionary<string, byte[]> Sources { get; } = [];

    public Task<Result<byte[]>> ReadDxvkDllAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sources.TryGetValue(sourcePath, out var dll)
            ? Result.Success(dll)
            : Result.Failure<byte[]>(new Error(DiagnosticCodes.FixSourceInvalid, $"{sourcePath} does not exist.")));
}
