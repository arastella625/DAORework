using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Catalog;

/// <summary>
/// The known-items catalog (ADR-0011): community fixes and mods DAORework can recognize, and the combinations known to
/// break. Loaded from data, so it is only trusted after <see cref="Validate"/>, which enforces ADR-0009's maintenance
/// rule: every entry has a source link, a license and at least one fingerprint.
/// </summary>
public sealed record KnownItemsCatalog(int SchemaVersion, IReadOnlyList<KnownItem> Items, IReadOnlyList<KnownConflict> Conflicts)
{
    public const int CurrentSchemaVersion = 1;

    public static Result<KnownItemsCatalog> Validate(KnownItemsCatalog? catalog)
    {
        if (catalog is null)
        {
            return Invalid("The catalog is empty.");
        }

        if (catalog.SchemaVersion != CurrentSchemaVersion)
        {
            return Invalid($"Schema version {catalog.SchemaVersion} is not supported (expected {CurrentSchemaVersion}).");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var problem = ItemsProblem(catalog.Items ?? [], ids) ?? ConflictsProblem(catalog.Conflicts ?? [], ids);
        if (problem is not null)
        {
            return Invalid(problem);
        }

        return Result.Success(catalog with { Items = catalog.Items ?? [], Conflicts = catalog.Conflicts ?? [] });
    }

    private static string? ItemsProblem(IReadOnlyList<KnownItem> items, HashSet<string> ids)
    {
        foreach (var item in items)
        {
            var problem = Problem(item);
            if (problem is not null)
            {
                return $"Entry '{item?.Id}': {problem}";
            }

            if (!ids.Add(item!.Id))
            {
                return $"Entry '{item.Id}' appears twice.";
            }
        }

        return null;
    }

    private static string? ConflictsProblem(IReadOnlyList<KnownConflict> conflicts, HashSet<string> ids) =>
        conflicts
            .Select(conflict => (conflict, Problem: Problem(conflict, ids)))
            .Where(c => c.Problem is not null)
            .Select(c => $"Conflict '{c.conflict?.First}' / '{c.conflict?.Second}': {c.Problem}")
            .FirstOrDefault();

    private static string? Problem(KnownItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Name))
        {
            return "an id and a name are required.";
        }

        if (!IsHttpsUrl(item.Source))
        {
            return "the source must be an https link.";
        }

        if (string.IsNullOrWhiteSpace(item.License))
        {
            return "a license is required.";
        }

        if (item.Fingerprints is null || item.Fingerprints.Count == 0)
        {
            return "at least one fingerprint is required.";
        }

        return item.Fingerprints.Select(Problem).FirstOrDefault(p => p is not null)
            ?? (item.Fingerprints.All(f => f.Kind == FingerprintKind.DownloadSha256)
                ? "a download hash cannot tell whether the item is installed; add a fingerprint found on disk."
                : null);
    }

    private static string? Problem(KnownItemFingerprint? fingerprint)
    {
        if (fingerprint is null || !Enum.IsDefined(fingerprint.Kind))
        {
            return "a fingerprint has no valid kind.";
        }

        if (fingerprint.Kind != FingerprintKind.LargeAddressAware && string.IsNullOrWhiteSpace(fingerprint.Value))
        {
            return $"the {fingerprint.Kind} fingerprint has no value.";
        }

        if (fingerprint.Kind == FingerprintKind.GameFile && !IsInstallRelative(fingerprint.Value))
        {
            return $"the game file '{fingerprint.Value}' must be a path inside the install directory.";
        }

        if (fingerprint.Marker is not null && fingerprint.Kind != FingerprintKind.GameFile)
        {
            return $"only a GameFile fingerprint can have a marker, not {fingerprint.Kind}.";
        }

        return DownloadProblem(fingerprint);
    }

    private static string? DownloadProblem(KnownItemFingerprint fingerprint)
    {
        if (fingerprint.Kind != FingerprintKind.DownloadSha256)
        {
            return fingerprint.Version is null ? null : $"only a DownloadSha256 fingerprint can have a version, not {fingerprint.Kind}.";
        }

        if (fingerprint.Value.Length != 64 || !fingerprint.Value.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)))
        {
            return $"the download hash '{fingerprint.Value}' must be a SHA-256 in 64 lowercase hex digits.";
        }

        return string.IsNullOrWhiteSpace(fingerprint.Version) ? $"the download hash '{fingerprint.Value}' needs the version of that download." : null;
    }

    private static string? Problem(KnownConflict? conflict, HashSet<string> ids)
    {
        if (conflict is null || !ids.Contains(conflict.First ?? string.Empty) || !ids.Contains(conflict.Second ?? string.Empty))
        {
            return "both sides must be catalog entries.";
        }

        if (string.Equals(conflict.First, conflict.Second, StringComparison.OrdinalIgnoreCase))
        {
            return "an entry cannot conflict with itself.";
        }

        // Report only (ROADMAP Phase 1): a conflict warns, it never blocks a launch the way an Error would.
        if (conflict.Severity is not (DiagnosticSeverity.Info or DiagnosticSeverity.Warning))
        {
            return "the severity must be Info or Warning.";
        }

        if (string.IsNullOrWhiteSpace(conflict.Reason))
        {
            return "a reason is required.";
        }

        return IsHttpsUrl(conflict.Source) ? null : "the source must be an https link.";
    }

    private static bool IsHttpsUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsInstallRelative(string path) =>
        !Path.IsPathRooted(path)
        && !path.Contains(':', StringComparison.Ordinal)
        && !path.Split('/', '\\').Any(segment => segment is ".." or "");

    private static Result<KnownItemsCatalog> Invalid(string message) =>
        Result.Failure<KnownItemsCatalog>(new Error(DiagnosticCodes.CatalogInvalid, $"The known-items catalog is invalid. {message}"));
}
