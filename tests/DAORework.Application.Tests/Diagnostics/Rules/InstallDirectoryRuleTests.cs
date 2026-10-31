using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class InstallDirectoryRuleTests
{
    private readonly MockFileSystem _fileSystem = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task EvaluateAsync_NotSet_WarnsMissing(string? directory)
    {
        var issues = await new InstallDirectoryRule(_fileSystem).EvaluateAsync(new RuntimeProfile { InstallDirectory = directory });

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.InstallDirectoryMissing, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_SetButAbsent_WarnsNotFound()
    {
        var issues = await new InstallDirectoryRule(_fileSystem).EvaluateAsync(new RuntimeProfile { InstallDirectory = TestPaths.Root("Games", "DAO") });

        Assert.Equal(DiagnosticCodes.InstallDirectoryNotFound, Assert.Single(issues).Code);
    }

    [Fact]
    public async Task EvaluateAsync_Exists_ReportsNothing()
    {
        var directory = TestPaths.Root("Games", "DAO");
        _fileSystem.AddDirectory(directory);

        var issues = await new InstallDirectoryRule(_fileSystem).EvaluateAsync(new RuntimeProfile { InstallDirectory = directory });

        Assert.Empty(issues);
    }
}
