using DAORework.Application.Documents;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// Two things OneDrive's Known Folder Move causes: the configured documents folder is missing while a Dragon Age
/// folder exists somewhere else, and the folder in use is synced by OneDrive.
/// </summary>
public sealed class DocumentsRedirectionRule : IDiagnosticRule
{
    private readonly DetectDocumentsFolders _detectDocumentsFolders;

    public DocumentsRedirectionRule(DetectDocumentsFolders detectDocumentsFolders)
    {
        _detectDocumentsFolders = detectDocumentsFolders;
    }

    public async Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var configured = profile.UserDocumentsDirectory;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return [];
        }

        var detection = (await _detectDocumentsFolders.ExecuteAsync(new DetectDocumentsFoldersRequest(), cancellationToken)).Value;
        var issues = new List<DiagnosticIssue>();

        var elsewhere = detection.Candidates.FirstOrDefault(c => c.Exists && !PathsEqual(c.Path, configured));
        var configuredExists = detection.Candidates.Any(c => c.Exists && PathsEqual(c.Path, configured));
        if (elsewhere is not null && !configuredExists)
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.DocumentsDirectoryElsewhere,
                $"The profile's documents folder is {configured}, but a Dragon Age documents folder exists at {elsewhere.Path}.",
                $"If your saves and mods are there, point the profile at it: profile set-path <name> documents \"{elsewhere.Path}\""));
        }

        if (DetectDocumentsFolders.IsUnderOneDrive(configured))
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.DocumentsOneDriveRedirected,
                $"The documents folder is synced by OneDrive: {configured}",
                "Saves and mods sync to the cloud. If the game fails to save or does not see mods, mark this folder 'Always keep on this device' in OneDrive."));
        }

        return issues;
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            left.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            right.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
