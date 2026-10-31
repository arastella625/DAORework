using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class GameExecutableRuleTests
{
    private static readonly string _exe = GamePaths.GameExecutable(TestPaths.Root("Games", "DAO"));

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_NotSet_ErrorsMissingWithRecommendation()
    {
        var issues = await new GameExecutableRule(_fileSystem).EvaluateAsync(new RuntimeProfile());

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.GameExecutableMissing, issue.Code);
        Assert.Equal(DiagnosticSeverity.Error, issue.Severity);
        Assert.Contains(GamePaths.GameExecutableFileName, issue.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_SetButAbsent_ErrorsNotFound()
    {
        var issues = await new GameExecutableRule(_fileSystem).EvaluateAsync(new RuntimeProfile { GameExecutablePath = _exe });

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.GameExecutableNotFound, issue.Code);
        Assert.Equal(DiagnosticSeverity.Error, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_Exists_ReportsNothing()
    {
        _fileSystem.AddFile(_exe, new MockFileData("exe"));

        var issues = await new GameExecutableRule(_fileSystem).EvaluateAsync(new RuntimeProfile { GameExecutablePath = _exe });

        Assert.Empty(issues);
    }
}
