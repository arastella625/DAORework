using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

/// <summary>P2.6-75 (ADR-0046): a profile whose documents folder isn't the one the game reads.</summary>
public sealed class GameDocumentsFolderRuleTests
{
    private static readonly string _elsewhere = TestPaths.Root("BioWare", "Dragon Age");

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeKnownFolders _knownFolders = new();

    private string Games => GamePaths.DefaultDocumentsDirectory(_knownFolders.MyDocuments);

    [Fact]
    public async Task EvaluateAsync_ProfileElsewhereAndGamesFolderExists_WarnsNamingBoth()
    {
        _fileSystem.AddDirectory(Games);
        _fileSystem.AddDirectory(_elsewhere);

        var issues = await Rule().EvaluateAsync(Profile(_elsewhere));

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.DocumentsNotTheGames, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
        Assert.Contains(Games, issue.Message, StringComparison.Ordinal);
        Assert.Contains(_elsewhere, issue.Message, StringComparison.Ordinal);
        Assert.Contains("Game folders", issue.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_ProfileUsesTheGamesFolder_ReportsNothing()
    {
        _fileSystem.AddDirectory(Games);

        var profile = Profile(Games.ToUpperInvariant() + Path.DirectorySeparatorChar);

        Assert.Empty(await Rule().EvaluateAsync(profile));
    }

    [Fact]
    public async Task EvaluateAsync_GamesFolderLinksToTheProfilesFolder_ReportsNothing()
    {
        _fileSystem.AddDirectory(_elsewhere);
        _fileSystem.AddDirectory(Path.GetDirectoryName(Games)!); // The game's BioWare folder, so the link has a parent.
        _fileSystem.Directory.CreateSymbolicLink(Games, _elsewhere);

        Assert.Empty(await Rule().EvaluateAsync(Profile(_elsewhere)));
    }

    [Fact]
    public async Task EvaluateAsync_ProfilesFolderLinksToTheGamesFolder_ReportsNothing()
    {
        _fileSystem.AddDirectory(Games);
        _fileSystem.AddDirectory(Path.GetDirectoryName(_elsewhere)!);
        _fileSystem.Directory.CreateSymbolicLink(_elsewhere, Games);

        Assert.Empty(await Rule().EvaluateAsync(Profile(_elsewhere)));
    }

    [Fact]
    public async Task EvaluateAsync_ProfilesFolderIsUnderALinkToTheGamesDocuments_ReportsNothing()
    {
        var linked = TestPaths.Root("Linked");
        _fileSystem.AddDirectory(Games);
        _fileSystem.Directory.CreateSymbolicLink(linked, _knownFolders.MyDocuments);

        Assert.Empty(await Rule().EvaluateAsync(Profile(GamePaths.DefaultDocumentsDirectory(linked))));
    }

    [Fact]
    public async Task EvaluateAsync_GamesFolderLinksSomewhereElse_StillWarns()
    {
        var third = TestPaths.Root("Elsewhere", "Dragon Age");
        _fileSystem.AddDirectory(third);
        _fileSystem.AddDirectory(_elsewhere);
        _fileSystem.AddDirectory(Path.GetDirectoryName(Games)!);
        _fileSystem.Directory.CreateSymbolicLink(Games, third);

        var issue = Assert.Single(await Rule().EvaluateAsync(Profile(_elsewhere)));
        Assert.Equal(DiagnosticCodes.DocumentsNotTheGames, issue.Code);
    }

    [Fact]
    public async Task EvaluateAsync_GamesFolderDoesNotExist_ReportsNothing()
    {
        _fileSystem.AddDirectory(_elsewhere);

        Assert.Empty(await Rule().EvaluateAsync(Profile(_elsewhere)));
    }

    [Fact]
    public async Task EvaluateAsync_NoDocumentsFolderSet_ReportsNothing()
    {
        _fileSystem.AddDirectory(Games);

        Assert.Empty(await Rule().EvaluateAsync(new RuntimeProfile()));
    }

    [Fact]
    public async Task EvaluateAsync_OnLinux_ReportsNothing()
    {
        _fileSystem.AddDirectory(Games);
        _fileSystem.AddDirectory(_elsewhere);

        var rule = new GameDocumentsFolderRule(_fileSystem, _knownFolders, new FakeHostPlatform { IsLinux = true }, NullLogger<GameDocumentsFolderRule>.Instance);

        Assert.Empty(await rule.EvaluateAsync(Profile(_elsewhere)));
    }

    private static RuntimeProfile Profile(string documents) => new() { Name = "Default", UserDocumentsDirectory = documents };

    private GameDocumentsFolderRule Rule() => new(_fileSystem, _knownFolders, new FakeHostPlatform(), NullLogger<GameDocumentsFolderRule>.Instance);
}
