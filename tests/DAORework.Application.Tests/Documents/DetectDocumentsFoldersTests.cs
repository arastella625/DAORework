using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Documents;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Documents;
using Xunit;

namespace DAORework.Application.Tests.Documents;

public sealed class DetectDocumentsFoldersTests
{
    private static readonly string _oneDrive = TestPaths.Root("Users", "Me", "OneDrive");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task ExecuteAsync_NoOneDriveAndNothingOnDisk_RecommendsTheKnownFolder()
    {
        var knownFolders = new FakeKnownFolders();

        var detection = (await Detect(knownFolders)).Value;

        Assert.Equal(GamePaths.DefaultDocumentsDirectory(knownFolders.MyDocuments), detection.Recommended.Path);
        Assert.Equal(DocumentsFolderSource.KnownFolder, detection.Recommended.Source);
        Assert.False(detection.Recommended.Exists);
        Assert.False(detection.RecommendedDiffersFromKnownFolder);
        Assert.Single(detection.Candidates);
    }

    [Fact]
    public async Task ExecuteAsync_KnownFolderMovedIntoOneDrive_ListsBothAndFlagsOneDrive()
    {
        var knownFolders = new FakeKnownFolders { MyDocuments = Path.Combine(_oneDrive, "Documents"), OneDrive = _oneDrive };

        var detection = (await Detect(knownFolders)).Value;

        Assert.Equal(2, detection.Candidates.Count);
        var known = Assert.Single(detection.Candidates, c => c.Source == DocumentsFolderSource.KnownFolder);
        Assert.True(known.IsOneDrive);
        Assert.Equal(GamePaths.DefaultDocumentsDirectory(Path.Combine(_oneDrive, "Documents")), known.Path);
        var classic = Assert.Single(detection.Candidates, c => c.Source == DocumentsFolderSource.UserProfile);
        Assert.False(classic.IsOneDrive);
    }

    [Fact]
    public async Task ExecuteAsync_SavesLeftInTheClassicFolder_RecommendsItOverTheEmptyKnownFolder()
    {
        var knownFolders = new FakeKnownFolders { MyDocuments = Path.Combine(_oneDrive, "Documents"), OneDrive = _oneDrive };
        var classic = GamePaths.DefaultDocumentsDirectory(Path.Combine(knownFolders.UserProfile, "Documents"));
        _fileSystem.AddDirectory(classic);

        var detection = (await Detect(knownFolders)).Value;

        Assert.Equal(classic, detection.Recommended.Path);
        Assert.True(detection.RecommendedDiffersFromKnownFolder);
    }

    [Fact]
    public async Task ExecuteAsync_BothExist_RecommendsTheKnownFolderTheGameUses()
    {
        var knownFolders = new FakeKnownFolders { MyDocuments = Path.Combine(_oneDrive, "Documents"), OneDrive = _oneDrive };
        _fileSystem.AddDirectory(GamePaths.DefaultDocumentsDirectory(knownFolders.MyDocuments));
        _fileSystem.AddDirectory(GamePaths.DefaultDocumentsDirectory(Path.Combine(knownFolders.UserProfile, "Documents")));

        var detection = (await Detect(knownFolders)).Value;

        Assert.Equal(DocumentsFolderSource.KnownFolder, detection.Recommended.Source);
        Assert.False(detection.RecommendedDiffersFromKnownFolder);
    }

    [Theory]
    [InlineData(@"C:\Users\Me\OneDrive\Documents\BioWare\Dragon Age", true)]
    [InlineData(@"C:\Users\Me\OneDrive - Contoso\Documents", true)]
    [InlineData(@"C:\Users\Me\Documents\BioWare\Dragon Age", false)]
    [InlineData(@"D:\Games\onedriver\Docs", false)]
    public void IsUnderOneDrive_LooksAtPathSegments(string path, bool expected)
    {
        Assert.Equal(expected, DetectDocumentsFolders.IsUnderOneDrive(path.Replace('\\', Path.DirectorySeparatorChar)));
    }

    private Task<Core.Common.Result<DocumentsDetection>> Detect(FakeKnownFolders knownFolders) =>
        new DetectDocumentsFolders(_fileSystem, knownFolders).ExecuteAsync(new DetectDocumentsFoldersRequest());
}
