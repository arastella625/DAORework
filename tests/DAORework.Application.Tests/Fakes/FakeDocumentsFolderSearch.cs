using DAORework.Application.Documents;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeDocumentsFolderSearch : IDocumentsFolderSearch
{
    public List<string> Found { get; } = [];

    public int Searches { get; private set; }

    public Task<IReadOnlyList<string>> FindAsync(CancellationToken cancellationToken = default)
    {
        Searches++;
        return Task.FromResult<IReadOnlyList<string>>(Found);
    }
}
