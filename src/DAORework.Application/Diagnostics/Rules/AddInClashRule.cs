using DAORework.Application.Mods.AddIns;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.AddIns;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// Reports each file two or more add-ins give the game (P2.5-23), and whose copy it uses: the add-in with the lower
/// Priority, or on equal priorities the one listed first in <c>AddIns.xml</c>. An archive that can't be read is
/// reported instead of stopping the check. Report only: DAORework changes no priority.
/// </summary>
public sealed class AddInClashRule : IDiagnosticRule
{
    private const int NamesShown = 10;

    private readonly FindAddInClashes _findAddInClashes;

    public AddInClashRule(FindAddInClashes findAddInClashes)
    {
        _findAddInClashes = findAddInClashes;
    }

    public async Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory))
        {
            return [];
        }

        if (!(await _findAddInClashes.ExecuteAsync(profile.UserDocumentsDirectory, cancellationToken)).TryGetValue(out var report, out var error))
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Warning, error.Code, error.Message, error.Recommendation)];
        }

        var issues = report.Clashes
            .Take(NamesShown)
            .Select(clash => new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.AddInsShareFile, AddInClashWords.Sentence(clash), AddInClashWords.Advice))
            .ToList();
        if (report.Clashes.Count > NamesShown)
        {
            var more = report.Clashes.Count - NamesShown;
            issues.Add(new DiagnosticIssue(DiagnosticSeverity.Warning, DiagnosticCodes.AddInsShareFile, more == 1 ? "1 more file is in more than one add-in." : $"{more} more files are in more than one add-in."));
        }

        if (report.Unreadable.Count > 0)
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.AddInsArchiveUnreadable,
                $"{Archives(report.Unreadable.Count)} in AddIns couldn't be read, so what's inside isn't compared with other add-ins: {string.Join(", ", report.Unreadable.Take(3))}{(report.Unreadable.Count > 3 ? ", …" : string.Empty)}.",
                "The add-in may be damaged. Download it again and reinstall it."));
        }

        return issues;
    }

    private static string Archives(int count) => count == 1 ? "1 archive" : $"{count} archives";
}
