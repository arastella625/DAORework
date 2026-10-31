namespace DAORework.Core.Catalog;

/// <summary>
/// A community fix, mod or tool the catalog can recognize (ADR-0009 tier 1, ADR-0011). <see cref="WhenMissing"/> is
/// the advice given when the item is not installed; items without it are only reported when found.
/// </summary>
public sealed record KnownItem(
    string Id,
    string Name,
    KnownItemKind Kind,
    string Source,
    string License,
    IReadOnlyList<KnownItemFingerprint> Fingerprints,
    string? WhenMissing = null);
