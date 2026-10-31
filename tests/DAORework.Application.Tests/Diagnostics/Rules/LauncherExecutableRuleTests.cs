using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class LauncherExecutableRuleTests
{
    private static readonly string _launcher = GamePaths.LauncherExecutable(TestPaths.Root("Games", "DAO"));

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_GameTargetAndNoLauncher_ReportsNothing()
    {
        var issues = await new LauncherExecutableRule(_fileSystem).EvaluateAsync(new RuntimeProfile { LaunchTarget = LaunchTarget.Game });

        Assert.Empty(issues);
    }

    [Fact]
    public async Task EvaluateAsync_LauncherSetButAbsent_Warns()
    {
        var issues = await new LauncherExecutableRule(_fileSystem).EvaluateAsync(new RuntimeProfile { LauncherExecutablePath = _launcher });

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.LauncherExecutableNotFound, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_OfficialLauncherTargetWithoutPath_Errors()
    {
        var issues = await new LauncherExecutableRule(_fileSystem).EvaluateAsync(new RuntimeProfile { LaunchTarget = LaunchTarget.OfficialLauncher });

        var issue = Assert.Single(issues);
        Assert.Equal(DiagnosticCodes.LauncherExecutableMissing, issue.Code);
        Assert.Equal(DiagnosticSeverity.Error, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_OfficialLauncherTargetWithExistingLauncher_ReportsNothing()
    {
        _fileSystem.AddFile(_launcher, new MockFileData("exe"));
        var profile = new RuntimeProfile { LaunchTarget = LaunchTarget.OfficialLauncher, LauncherExecutablePath = _launcher };

        var issues = await new LauncherExecutableRule(_fileSystem).EvaluateAsync(profile);

        Assert.Empty(issues);
    }
}
