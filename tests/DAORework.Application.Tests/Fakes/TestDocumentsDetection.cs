using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Documents;
using DAORework.Application.Profiles;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Builds <see cref="DetectDocumentsFolders"/> with no saved profiles and an empty drive search, unless a test gives its own.</summary>
internal static class TestDocumentsDetection
{
    public static DetectDocumentsFolders Create(
        IFileSystem fileSystem,
        IKnownFolders knownFolders,
        IProfileRepository? profiles = null,
        IDocumentsFolderSearch? search = null) =>
        new(
            fileSystem,
            knownFolders,
            new ProfileDocumentsFolders(profiles ?? new InMemoryProfileRepository()),
            search ?? new FakeDocumentsFolderSearch());
}
