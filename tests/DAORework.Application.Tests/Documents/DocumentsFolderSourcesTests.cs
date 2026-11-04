using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using DAORework.Application.Documents;
using DAORework.Application.Profiles;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Documents;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Application.Tests.Documents;

/// <summary>The sources after the usual three: the folders profiles name, then the drive search (ADR-0032).</summary>
public sealed class DocumentsFolderSourcesTests
{
    private static readonly string _moved = TestPaths.Root("Games", "BioWare", "Dragon Age");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeKnownFolders _knownFolders = new();
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly FakeDocumentsFolderSearch _search = new();

    private string KnownFolder => GamePaths.DefaultDocumentsDirectory(_knownFolders.MyDocuments);

    [Fact]
    public async Task ExecuteAsync_FolderOnlyAProfileNames_RecommendsItWithoutSearching()
    {
        _fileSystem.AddDirectory(_moved);
        NameInAProfile(_moved);

        var detection = await Detect();

        Assert.Equal(_moved, detection.Recommended.Path);
        Assert.Equal(DocumentsFolderSource.Profile, detection.Recommended.Source);
        Assert.True(detection.RecommendedDiffersFromKnownFolder);
        Assert.Equal(0, _search.Searches);
    }

    [Fact]
    public async Task ExecuteAsync_KnownFolderAndAProfilesFolderBothExist_StillRecommendsTheKnownFolder()
    {
        _fileSystem.AddDirectory(KnownFolder);
        _fileSystem.AddDirectory(_moved);
        NameInAProfile(_moved);

        var detection = await Detect();

        Assert.Equal(DocumentsFolderSource.KnownFolder, detection.Recommended.Source);
        Assert.Contains(detection.Candidates, c => c.Path == _moved && c.Exists);
    }

    [Fact]
    public async Task ExecuteAsync_ProfileNamesTheKnownFolderWithATrailingSeparator_ListsItOnce()
    {
        NameInAProfile(KnownFolder + Path.DirectorySeparatorChar);

        var detection = await Detect();

        Assert.Equal(DocumentsFolderSource.KnownFolder, Assert.Single(detection.Candidates).Source);
    }

    [Fact]
    public async Task ExecuteAsync_NothingExistsAnywhereElse_RecommendsWhatTheSearchFound()
    {
        _fileSystem.AddDirectory(_moved);
        _search.Found.Add(_moved);

        var detection = await Detect();

        Assert.Equal(_moved, detection.Recommended.Path);
        Assert.Equal(DocumentsFolderSource.DriveSearch, detection.Recommended.Source);
        Assert.True(detection.Recommended.Exists);
    }

    [Fact]
    public async Task ExecuteAsync_ProfileNamesAFolderThatIsGone_LeavesItOutAndStillSearches()
    {
        var gone = TestPaths.Root("Old", "BioWare", "Dragon Age");
        NameInAProfile(gone);
        _fileSystem.AddDirectory(_moved);
        _search.Found.Add(_moved);

        var detection = await Detect();

        Assert.Equal(_moved, detection.Recommended.Path);
        Assert.Equal(1, _search.Searches);
        Assert.DoesNotContain(detection.Candidates, c => c.Path == gone);
    }

    [Fact]
    public async Task ExecuteAsync_FolderInTheUsualPlace_DoesNotSearch()
    {
        _fileSystem.AddDirectory(KnownFolder);

        await Detect();

        Assert.Equal(0, _search.Searches);
    }

    [Fact]
    public async Task ExecuteAsync_SearchFindsNothing_RecommendsTheKnownFolder()
    {
        var detection = await Detect();

        Assert.Equal(1, _search.Searches);
        Assert.Equal(DocumentsFolderSource.KnownFolder, detection.Recommended.Source);
        Assert.False(detection.Recommended.Exists);
    }

    [Theory]
    [InlineData(typeof(InvalidDataException))]
    [InlineData(typeof(JsonException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public async Task ReadAsync_AProfileCannotBeRead_ReturnsTheOthers(Type failure)
    {
        var profiles = new UnreadableProfileRepository(_profiles, "Broken", (Exception)Activator.CreateInstance(failure)!);
        NameInAProfile(_moved);

        var folders = await Reader(profiles).ReadAsync();

        Assert.Equal(_moved, Assert.Single(folders));
    }

    [Fact]
    public async Task ReadAsync_ProfileWithoutADocumentsFolder_NamesNone()
    {
        _profiles.Profiles["Empty"] = new RuntimeProfile { Name = "Empty" };

        Assert.Empty(await Reader(_profiles).ReadAsync());
    }

    private static ProfileDocumentsFolders Reader(IProfileRepository profiles) => new(profiles, NullLogger<ProfileDocumentsFolders>.Instance);

    private void NameInAProfile(string folder) =>
        _profiles.Profiles["Default"] = new RuntimeProfile { Name = "Default", UserDocumentsDirectory = folder };

    private async Task<DocumentsDetection> Detect() =>
        (await TestDocumentsDetection.Create(_fileSystem, _knownFolders, _profiles, _search).ExecuteAsync(new DetectDocumentsFoldersRequest())).Value;
}
