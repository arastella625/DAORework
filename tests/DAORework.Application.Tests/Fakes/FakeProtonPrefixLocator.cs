using DAORework.Application.Documents;

namespace DAORework.Application.Tests.Fakes;

internal sealed class FakeProtonPrefixLocator : IProtonPrefixLocator
{
    public List<string> Prefixes { get; } = [];

    public List<string> Documents { get; } = [];

    public IReadOnlyList<string> FindPrefixes() => Prefixes;

    public IReadOnlyList<string> FindDocumentsFolders() => Documents;
}
