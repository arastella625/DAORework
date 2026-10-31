using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class DocumentsDirectoryRuleTests
{
    private static readonly string _documents = TestPaths.Root("Docs");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_NotSet_ReportsNothing()
    {
        var issues = await new DocumentsDirectoryRule(_fileSystem).EvaluateAsync(new RuntimeProfile());

        Assert.Empty(issues);
    }

    [Fact]
    public async Task EvaluateAsync_SetButAbsent_Warns()
    {
        var issues = await new DocumentsDirectoryRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.DocumentsDirectoryNotFound, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_Exists_ReportsNothing()
    {
        _fileSystem.AddDirectory(_documents);

        var issues = await new DocumentsDirectoryRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });

        Assert.Empty(issues);
    }
}
