using System.IO.Abstractions;
using DAORework.Core.Diagnostics;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>A configured launcher must exist (warning), and the OfficialLauncher target needs one (error).</summary>
public sealed class LauncherExecutableRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;

    public LauncherExecutableRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        var issues = new List<DiagnosticIssue>();
        var path = profile.LauncherExecutablePath;

        if (!string.IsNullOrWhiteSpace(path) && !_fileSystem.File.Exists(path))
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.LauncherExecutableNotFound,
                $"The configured official launcher executable was not found: {path}",
                "This only blocks launching through the official launcher target."));
        }

        if (profile.LaunchTarget == LaunchTarget.OfficialLauncher && string.IsNullOrWhiteSpace(path))
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Error,
                DiagnosticCodes.LauncherExecutableMissing,
                "LaunchTarget is OfficialLauncher, but LauncherExecutablePath is not set.",
                "Set LaunchTarget to Game or recreate the profile using installation detection."));
        }

        return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(issues);
    }
}
