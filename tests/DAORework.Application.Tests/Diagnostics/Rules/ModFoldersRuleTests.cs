using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class ModFoldersRuleTests
{
    private static readonly string _documents = TestPaths.Root("Docs");

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_DocumentsMissing_ReportsNothing()
    {
        var issues = await new ModFoldersRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });

        Assert.Empty(issues);
    }

    [Fact]
    public async Task EvaluateAsync_VanillaInstall_WarnsBothFoldersMissing()
    {
        _fileSystem.AddDirectory(_documents);

        var issues = await new ModFoldersRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });

        Assert.Equal([DiagnosticCodes.OverrideDirectoryNotFound, DiagnosticCodes.AddInsDirectoryNotFound], issues.Select(i => i.Code));
        Assert.All(issues, i => Assert.Equal(DiagnosticSeverity.Warning, i.Severity));
    }

    [Fact]
    public async Task EvaluateAsync_OverridePresentOnly_ReportsFoundAndNotFound()
    {
        _fileSystem.AddDirectory(GamePaths.OverrideDirectory(_documents));

        var issues = await new ModFoldersRule(_fileSystem).EvaluateAsync(new RuntimeProfile { UserDocumentsDirectory = _documents });

        Assert.Equal([DiagnosticCodes.OverrideDirectoryFound, DiagnosticCodes.AddInsDirectoryNotFound], issues.Select(i => i.Code));
        Assert.Equal(DiagnosticSeverity.Info, issues[0].Severity);
    }
}
