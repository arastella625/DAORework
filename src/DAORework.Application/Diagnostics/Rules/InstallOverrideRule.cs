using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// Reports the install folder's own override (P2.5-22), <c>&lt;install&gt;\packages\core\override</c>: older guides put
/// mods there, and the game loads it as well as the documents folder's. DAORework does not manage or adopt what is
/// there, so the report says how much there is and which file names the documents folder's override has too, where
/// the game uses one copy. Report only.
/// </summary>
public sealed class InstallOverrideRule : IDiagnosticRule
{
    private const int NamesShown = 10;

    private readonly IFileSystem _fileSystem;

    public InstallOverrideRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.InstallDirectory))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([]);
        }

        var installOverride = GamePaths.OverrideDirectory(profile.InstallDirectory);
        var files = Files(installOverride);
        if (files.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([]);
        }

        var issues = new List<DiagnosticIssue>
        {
            new(
                DiagnosticSeverity.Info,
                DiagnosticCodes.InstallOverrideFiles,
                $"The install folder's override has {files.Count} file(s): {installOverride}. The game loads them as well as the documents folder's override; DAORework does not manage them.",
                "To have DAORework manage a mod there, move its folder to the documents folder's override, then adopt it on the Mods tab."),
        };
        if (!string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory))
        {
            issues.AddRange(Shared(files, Files(GamePaths.OverrideDirectory(profile.UserDocumentsDirectory))));
        }

        return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(issues);
    }

    private static IEnumerable<DiagnosticIssue> Shared(List<string> installFiles, List<string> documentsFiles)
    {
        var inDocuments = documentsFiles.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shared = installFiles.Select(Path.GetFileName).OfType<string>().Where(inDocuments.Contains).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
        return shared.Take(NamesShown).Select(name => new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.InstallOverrideShared,
                $"{name} is in both the install folder's override and the documents folder's; the game uses one copy.{(SharedFileMeaning.For(name) is { } meaning ? " " + meaning : string.Empty)}",
                "Keep one copy: remove the one you no longer want, or move the install folder's mod to the documents folder and adopt it."))
            .Concat(shared.Count > NamesShown
                ? [new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.InstallOverrideShared, $"{shared.Count - NamesShown} more file name(s) are in both overrides.")]
                : []);
    }

    private List<string> Files(string folder) =>
        _fileSystem.Directory.Exists(folder) ? [.. _fileSystem.Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)] : [];
}
