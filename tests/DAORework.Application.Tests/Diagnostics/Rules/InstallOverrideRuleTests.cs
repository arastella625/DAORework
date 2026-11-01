using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

/// <summary>The install folder's own override (P2.5-22): reported, and each file name the documents folder's override shares.</summary>
public sealed class InstallOverrideRuleTests
{
    private static readonly string _install = TestPaths.Root("Game");
    private static readonly string _documents = TestPaths.Root("Docs");

    private readonly MockFileSystem _fileSystem = new();

    private RuntimeProfile Profile => new() { InstallDirectory = _install, UserDocumentsDirectory = _documents };

    [Fact]
    public async Task EvaluateAsync_NoInstallOverride_ReportsNothing()
    {
        _fileSystem.AddDirectory(_install);

        Assert.Empty(await new InstallOverrideRule(_fileSystem).EvaluateAsync(Profile));
    }

    [Fact]
    public async Task EvaluateAsync_FilesThere_SaysHowManyAndThatTheGameLoadsThem()
    {
        _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_install), "Old Hair", "hair.dds"), new MockFileData("x"));
        _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_install), "fix.ncs"), new MockFileData("x"));

        var issue = Assert.Single(await new InstallOverrideRule(_fileSystem).EvaluateAsync(Profile));

        Assert.Equal(DiagnosticCodes.InstallOverrideFiles, issue.Code);
        Assert.Equal(DiagnosticSeverity.Info, issue.Severity);
        Assert.Contains("has 2 file(s)", issue.Message, StringComparison.Ordinal);
        Assert.Contains("move its folder to the documents folder's override", issue.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_ANameBothOverridesHave_IsAWarningThatSaysWhatItMeans()
    {
        _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_install), "ABI_base.gda"), new MockFileData("old"));
        _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_documents), "DAORework", "rebalance-1", "abi_base.gda"), new MockFileData("new"));

        var issues = await new InstallOverrideRule(_fileSystem).EvaluateAsync(Profile);

        var shared = Assert.Single(issues, i => i.Code == DiagnosticCodes.InstallOverrideShared);
        Assert.Equal(DiagnosticSeverity.Warning, shared.Severity);
        Assert.StartsWith("ABI_base.gda is in both the install folder's override and the documents folder's; the game uses one copy. It is a data table", shared.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_ManySharedNames_ListsTenAndCountsTheRest()
    {
        for (var i = 0; i < 12; i++)
        {
            _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_install), $"tex_{i:00}.dds"), new MockFileData("a"));
            _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(_documents), $"tex_{i:00}.dds"), new MockFileData("b"));
        }

        var shared = (await new InstallOverrideRule(_fileSystem).EvaluateAsync(Profile)).Where(i => i.Code == DiagnosticCodes.InstallOverrideShared).ToList();

        Assert.Equal(11, shared.Count);
        Assert.Equal("2 more file name(s) are in both overrides.", shared[^1].Message);
    }
}
