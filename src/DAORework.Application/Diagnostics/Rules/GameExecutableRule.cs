using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>The game executable must be set and exist; without it nothing can launch.</summary>
public sealed class GameExecutableRule : IDiagnosticRule
{
    private static readonly string _recommendation = $"Expected file is usually: {GamePaths.GameExecutableRelativePath}";

    private readonly IFileSystem _fileSystem;

    public GameExecutableRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        var path = profile.GameExecutablePath;

        IReadOnlyList<DiagnosticIssue> issues = string.IsNullOrWhiteSpace(path)
            ? [Error(DiagnosticCodes.GameExecutableMissing, "The Dragon Age: Origins game executable path is not set.")]
            : _fileSystem.File.Exists(path)
                ? []
                : [Error(DiagnosticCodes.GameExecutableNotFound, $"The configured game executable was not found: {path}")];

        return Task.FromResult(issues);
    }

    private static DiagnosticIssue Error(string code, string message) => new(DiagnosticSeverity.Error, code, message, _recommendation);
}
