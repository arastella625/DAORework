using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class ConfigRuleTests
{
    private static readonly string _documents = TestPaths.Root("Docs", "BioWare", "Dragon Age");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_DocumentsMissing_ReportsNothing()
    {
        Assert.Empty(await Evaluate());
    }

    [Fact]
    public async Task EvaluateAsync_NoSettingsYet_SaysTheGameWritesIt()
    {
        _fileSystem.AddDirectory(_documents);

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.ConfigNotFound, issue.Code);
        Assert.Equal(DiagnosticSeverity.Info, issue.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_NormalResolution_ReportsIt()
    {
        AddSettings("[Graphics]\nWidth=2560\nHeight=1440\n");

        var issue = Assert.Single(await Evaluate());

        Assert.Equal(DiagnosticCodes.ConfigFound, issue.Code);
        Assert.Contains("2560x1440", issue.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Width=0\nHeight=0")]
    [InlineData("Width=99999\nHeight=1080")]
    public async Task EvaluateAsync_ImplausibleResolution_Warns(string values)
    {
        AddSettings("[Graphics]\n" + values);

        var issues = await Evaluate();

        Assert.Contains(issues, i => i.Code == DiagnosticCodes.ConfigResolutionSuspicious && i.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateAsync_NoResolutionKeys_SaysSo()
    {
        AddSettings("[Sound]\nVolume=80");

        var issue = Assert.Single(await Evaluate());

        Assert.Contains("resolution not recorded", issue.Message, StringComparison.Ordinal);
    }

    private void AddSettings(string text) => _fileSystem.AddFile(GamePaths.SettingsFile(_documents), new MockFileData(text));

    private Task<IReadOnlyList<DiagnosticIssue>> Evaluate() =>
        new ConfigRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });
}
