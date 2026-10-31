using System.IO.Abstractions;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>A configured documents directory should exist. An unset one is not reported: the game creates it on first launch.</summary>
public sealed class DocumentsDirectoryRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;

    public DocumentsDirectoryRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        var path = profile.UserDocumentsDirectory;

        IReadOnlyList<DiagnosticIssue> issues = !string.IsNullOrWhiteSpace(path) && !_fileSystem.Directory.Exists(path)
            ?
            [
                new DiagnosticIssue(
                    DiagnosticSeverity.Warning,
                    DiagnosticCodes.DocumentsDirectoryNotFound,
                    $"The configured DAO documents directory was not found: {path}",
                    "The directory may be created by the game after first launch."),
            ]
            : [];

        return Task.FromResult(issues);
    }
}
