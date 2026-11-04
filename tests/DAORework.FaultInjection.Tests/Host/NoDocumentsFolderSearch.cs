using DAORework.Application.Documents;

namespace DAORework.FaultInjection.Tests.Host;

/// <summary>Keeps these tests, which run on the real file system, off the machine's drives (ADR-0032).</summary>
internal sealed class NoDocumentsFolderSearch : IDocumentsFolderSearch
{
    public Task<IReadOnlyList<string>> FindAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
