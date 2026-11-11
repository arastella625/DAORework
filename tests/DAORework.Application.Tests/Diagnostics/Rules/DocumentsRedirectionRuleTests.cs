using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Documents;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class DocumentsRedirectionRuleTests
{
    private static readonly string _oneDrive = TestPaths.Root("Users", "Me", "OneDrive");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeKnownFolders _redirected = new() { MyDocuments = Path.Combine(_oneDrive, "Documents"), OneDrive = _oneDrive };

    private string Classic => GamePaths.DefaultDocumentsDirectory(Path.Combine(_redirected.UserProfile, "Documents"));

    private string InOneDrive => GamePaths.DefaultDocumentsDirectory(_redirected.MyDocuments);

    [Fact]
    public async Task EvaluateAsync_NotSet_ReportsNothing()
    {
        Assert.Empty(await Rule(_redirected).EvaluateAsync(new RuntimeProfile()));
    }

    [Fact]
    public async Task EvaluateAsync_ConfiguredFolderMissingButClassicOneExists_PointsAtIt()
    {
        _fileSystem.AddDirectory(Classic);

        var issues = await Rule(_redirected).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = InOneDrive });

        var elsewhere = Assert.Single(issues, i => i.Code == DiagnosticCodes.DocumentsDirectoryElsewhere);
        Assert.Equal(DiagnosticSeverity.Warning, elsewhere.Severity);
        Assert.Contains(Classic, elsewhere.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_ConfiguredFolderUnderOneDrive_WarnsAboutSyncing()
    {
        _fileSystem.AddDirectory(InOneDrive);

        var issues = await Rule(_redirected).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = InOneDrive });

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.DocumentsOneDriveRedirected, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_ClassicFolderConfiguredAndPresent_ReportsNothing()
    {
        _fileSystem.AddDirectory(Classic);

        Assert.Empty(await Rule(_redirected).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = Classic + Path.DirectorySeparatorChar }));
    }

    [Fact]
    public async Task EvaluateAsync_NoOneDriveAtAll_ReportsNothing()
    {
        var plain = new FakeKnownFolders();
        var documents = GamePaths.DefaultDocumentsDirectory(plain.MyDocuments);

        Assert.Empty(await Rule(plain).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = documents }));
    }

    [Fact]
    public async Task EvaluateAsync_ProfilesFolderOnAnotherDriveExists_DoesNotSayItIsElsewhere()
    {
        var moved = TestPaths.Root("Games", "BioWare", "Dragon Age");
        _fileSystem.AddDirectory(moved);
        _fileSystem.AddDirectory(Classic);
        var profile = new RuntimeProfile { Name = "Default", UserDocumentsDirectory = moved };
        var profiles = new InMemoryProfileRepository();
        profiles.Profiles[profile.Name] = profile;
        var rule = new DocumentsRedirectionRule(TestDocumentsDetection.Create(_fileSystem, _redirected, profiles), SameFolder());

        Assert.Empty(await rule.EvaluateAsync(profile));
    }

    [Fact]
    public async Task EvaluateAsync_ConfiguredFolderGoneAndTheSearchFindsOne_PointsAtIt()
    {
        var moved = TestPaths.Root("Games", "BioWare", "Dragon Age");
        _fileSystem.AddDirectory(moved);
        var search = new FakeDocumentsFolderSearch();
        search.Found.Add(moved);
        var rule = new DocumentsRedirectionRule(TestDocumentsDetection.Create(_fileSystem, new FakeKnownFolders(), search: search), SameFolder());

        var issues = await rule.EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = TestPaths.Root("Old", "BioWare", "Dragon Age") });

        var elsewhere = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.DocumentsDirectoryElsewhere, elsewhere.Code);
        Assert.Contains(moved, elsewhere.Recommendation, StringComparison.Ordinal);
    }

    private DocumentsRedirectionRule Rule(FakeKnownFolders knownFolders) => new(TestDocumentsDetection.Create(_fileSystem, knownFolders), SameFolder());

    private SameFolder SameFolder() => new(_fileSystem, NullLogger<SameFolder>.Instance);
}
