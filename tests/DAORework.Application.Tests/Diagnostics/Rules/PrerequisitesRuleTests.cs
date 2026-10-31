using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class PrerequisitesRuleTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeKnownFolders _knownFolders = new();

    [Fact]
    public async Task EvaluateAsync_NothingInstalled_WarnsForBoth()
    {
        var issues = await new PrerequisitesRule(_fileSystem, _knownFolders).EvaluateAsync(new RuntimeProfile());

        Assert.Equal([DiagnosticCodes.DirectXRuntimeMissing, DiagnosticCodes.VcRedistMissing], issues.Select(i => i.Code));
        Assert.All(issues, i => Assert.Equal(DiagnosticSeverity.Warning, i.Severity));
        Assert.All(issues, i => Assert.False(string.IsNullOrWhiteSpace(i.Recommendation)));
    }

    [Fact]
    public async Task EvaluateAsync_BothInstalled_ReportsBothFound()
    {
        _fileSystem.AddFile(Path.Combine(_knownFolders.SystemX86, PrerequisitesRule.DirectXProbeDll), new MockFileData("dll"));
        _fileSystem.AddDirectory(Path.Combine(_knownFolders.Windows, "WinSxS", PrerequisitesRule.VcRedistSxsPrefix + "1fc8b3b9a1e18e3b_8.0.50727.6195_none_d09154e044272b9a"));

        var issues = await new PrerequisitesRule(_fileSystem, _knownFolders).EvaluateAsync(new RuntimeProfile());

        Assert.Equal([DiagnosticCodes.DirectXRuntimeFound, DiagnosticCodes.VcRedistFound], issues.Select(i => i.Code));
        Assert.All(issues, i => Assert.Equal(DiagnosticSeverity.Info, i.Severity));
    }

    [Fact]
    public async Task EvaluateAsync_NotWindows_SaysItWasNotChecked()
    {
        var elsewhere = new FakeKnownFolders { Windows = string.Empty, SystemX86 = string.Empty };

        var issue = Assert.Single(await new PrerequisitesRule(_fileSystem, elsewhere).EvaluateAsync(new RuntimeProfile()));

        Assert.Equal(DiagnosticCodes.PrerequisitesNotChecked, issue.Code);
    }
}
