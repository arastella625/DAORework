using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeFixLibrary : IFixLibrary
{
    public List<FixEntry> Entries { get; } = [];

    public Dictionary<string, byte[]> Contents { get; } = [];

    public bool FailAdd { get; set; }

    public Task<IReadOnlyList<FixEntry>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FixEntry>>(Entries.ToList());

    public Task<Result<FixEntry>> AddAsync(FixEntry entry, IReadOnlyDictionary<string, byte[]> contents, CancellationToken cancellationToken = default)
    {
        if (FailAdd)
        {
            return Task.FromResult(Result.Failure<FixEntry>(new Error(DiagnosticCodes.FixLibraryFailed, "disk full")));
        }

        if (Entries.Find(e => e.Id == entry.Id) is { } existing)
        {
            return Task.FromResult(Result.Success(existing));
        }

        Entries.Add(entry);
        foreach (var (path, content) in contents)
        {
            Contents[$"{entry.Id}/{path}"] = content;
        }

        return Task.FromResult(Result.Success(entry));
    }
}
