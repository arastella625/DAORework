using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Application.Tests.Fixes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

/// <summary>ADR-0023: Steam's protected copy is named, and a patched one is warned about, because Steam will not start it.</summary>
public sealed class SteamProtectedExecutableRuleTests
{
    private static readonly string _exe = TestPaths.Root("SteamLibrary", "steamapps", "common", "Dragon Age Ultimate Edition", "bin_ship", "daorigins.exe");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task ProtectedAndUntouched_SaysThePatchIsNotApplied()
    {
        _fileSystem.AddFile(_exe, new MockFileData(TestExecutable.Build(steamProtected: true)));

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.SteamExecutableProtected, issue.Code);
        Assert.Equal(DiagnosticSeverity.Info, issue.Severity);
    }

    [Fact]
    public async Task ProtectedButPatched_WarnsThatSteamWillNotStartIt()
    {
        _fileSystem.AddFile(_exe, new MockFileData(TestExecutable.Build(largeAddressAware: true, steamProtected: true)));

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.SteamExecutablePatched, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
        Assert.Contains("fixes remove laa", issue.Recommendation, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotProtected_SaysNothing(bool largeAddressAware)
    {
        _fileSystem.AddFile(_exe, new MockFileData(TestExecutable.Build(largeAddressAware)));

        Assert.Empty(await Evaluate());
    }

    [Fact]
    public async Task NoExecutable_SaysNothing()
    {
        Assert.Empty(await Evaluate());
    }

    private Task<IReadOnlyList<DiagnosticIssue>> Evaluate() =>
        new SteamProtectedExecutableRule(_fileSystem).EvaluateAsync(new RuntimeProfile { GameExecutablePath = _exe });
}
