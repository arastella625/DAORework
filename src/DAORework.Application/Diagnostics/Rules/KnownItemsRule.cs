using DAORework.Application.Catalog;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// Reports the community fixes and mods from the known-items catalog (ADR-0009 tier 1): what is installed and in
/// which version, combinations known to break, a DLL the 32-bit game cannot load, and the fixes worth having that are
/// missing. Report only, like every Phase 1 rule.
/// </summary>
public sealed class KnownItemsRule : IDiagnosticRule
{
    private readonly DetectKnownItems _detectKnownItems;

    public KnownItemsRule(DetectKnownItems detectKnownItems)
    {
        _detectKnownItems = detectKnownItems;
    }

    public async Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var result = await _detectKnownItems.ExecuteAsync(new DetectKnownItemsRequest(profile), cancellationToken);
        if (!result.TryGetValue(out var scan, out var error))
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Warning, error.Code, error.Message, error.Recommendation)];
        }

        var issues = new List<DiagnosticIssue> { Summary(scan) };
        issues.AddRange(scan.Found
            .SelectMany(found => found.Matches)
            .Where(match => match.WrongArchitecture is not null)
            .Select(match => new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.KnownItemWrongArchitecture,
                match.WrongArchitecture!,
                "Replace it with the 32-bit (x32) build of the same release, or the game will not start.")));
        issues.AddRange(scan.Conflicts.Select(conflict => Conflict(conflict, scan)));
        issues.AddRange(scan.Missing
            .Where(item => item.WhenMissing is not null)
            .Select(item => new DiagnosticIssue(
                DiagnosticSeverity.Info,
                DiagnosticCodes.KnownItemSuggested,
                $"{item.Name} is not installed.",
                $"{item.WhenMissing} Source: {item.Source}")));
        issues.AddRange(scan.Unresolved.Select(Unresolved));
        return issues;
    }

    private static DiagnosticIssue Summary(KnownItemsScan scan)
    {
        // Official DLC is already listed by DlcRule; the catalog has it only so adoption leaves it alone (ADR-0015).
        var found = scan.Found.Where(f => f.Item.Kind != KnownItemKind.Official).ToList();
        if (found.Count == 0)
        {
            return new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.KnownItemsNone, "None of the fixes and mods DAORework knows about were found.");
        }

        var names = found.Select(f => $"{f.DisplayName} ({f.Matches[0].Location})");
        return new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.KnownItemsFound, $"Known fixes and mods installed: {string.Join("; ", names)}.");
    }

    private static DiagnosticIssue Conflict(KnownConflict conflict, KnownItemsScan scan)
    {
        string Name(string id) => scan.Found.First(f => string.Equals(f.Item.Id, id, StringComparison.OrdinalIgnoreCase)).Item.Name;
        return new DiagnosticIssue(
            conflict.Severity,
            DiagnosticCodes.KnownItemConflict,
            $"{Name(conflict.First)} and {Name(conflict.Second)} are both installed. {conflict.Reason}",
            $"Source: {conflict.Source}");
    }

    private static DiagnosticIssue Unresolved(Error error) => new(
        error.Code == DiagnosticCodes.KnownItemUnrecognized ? DiagnosticSeverity.Info : DiagnosticSeverity.Warning,
        error.Code,
        error.Message,
        error.Recommendation);
}
