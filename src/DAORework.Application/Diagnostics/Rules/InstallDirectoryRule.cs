using System.IO.Abstractions;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>The install directory should be set and exist. Either problem is a warning: launching only needs the executable.</summary>
public sealed class InstallDirectoryRule : IDiagnosticRule
{
    private const string Recommendation = "Run installation detection or recreate the runtime profile.";

    private readonly IFileSystem _fileSystem;

    public InstallDirectoryRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        var path = profile.InstallDirectory;

        IReadOnlyList<DiagnosticIssue> issues = string.IsNullOrWhiteSpace(path)
            ? [Warning(DiagnosticCodes.InstallDirectoryMissing, "The Dragon Age: Origins install directory is not set.")]
            : _fileSystem.Directory.Exists(path)
                ? []
                : [Warning(DiagnosticCodes.InstallDirectoryNotFound, $"The configured install directory was not found: {path}")];

        return Task.FromResult(issues);
    }

    private static DiagnosticIssue Warning(string code, string message) => new(DiagnosticSeverity.Warning, code, message, Recommendation);
}
