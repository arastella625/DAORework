using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Diagnostics;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Documents;

namespace DAORework.Application.Tests.Fakes;

/// <summary>The production rule set in production order, for tests that need the whole pipeline.</summary>
internal static class TestDiagnosticRules
{
    public static IReadOnlyList<IDiagnosticRule> All(IFileSystem fileSystem) => All(fileSystem, new FakeKnownFolders());

    public static IReadOnlyList<IDiagnosticRule> All(IFileSystem fileSystem, IKnownFolders knownFolders) =>
    [
        new InstallDirectoryRule(fileSystem),
        new GameExecutableRule(fileSystem),
        new LauncherExecutableRule(fileSystem),
        new DocumentsDirectoryRule(fileSystem),
        new DocumentsRedirectionRule(new DetectDocumentsFolders(fileSystem, knownFolders)),
        new ModFoldersRule(fileSystem),
        new LargeAddressAwareRule(fileSystem),
        new SteamProtectedExecutableRule(fileSystem),
        new PrerequisitesRule(fileSystem, knownFolders),
        new ConfigRule(fileSystem),
        new SavesRule(fileSystem),
        new DlcRule(fileSystem),
    ];
}
