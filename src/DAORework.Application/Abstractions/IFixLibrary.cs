using DAORework.Core.Common;
using DAORework.Core.Fixes;

namespace DAORework.Application.Abstractions;

/// <summary>The fixes held in DAORework's library (ADR-0013), each in <c>library/fixes/{id}/</c> with its <c>fix.json</c>.</summary>
public interface IFixLibrary
{
    Task<IReadOnlyList<FixEntry>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the entry with <paramref name="contents"/>, one per file path in it, each checked against the entry's hash.
    /// An entry already in the library is returned as it is.
    /// </summary>
    Task<Result<FixEntry>> AddAsync(FixEntry entry, IReadOnlyDictionary<string, byte[]> contents, CancellationToken cancellationToken = default);
}
