using DAORework.Application.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Catalog;

/// <summary>
/// Checks a profile's install and documents folder against every entry in the known-items catalog (Composite over
/// the <see cref="IFingerprintMatcher"/>s, ADR-0011). Only reads; nothing is installed or changed.
/// </summary>
public sealed class DetectKnownItems
{
    private readonly IKnownItemsCatalogSource _catalogSource;
    private readonly Dictionary<FingerprintKind, IFingerprintMatcher> _matchers;

    public DetectKnownItems(IKnownItemsCatalogSource catalogSource, IEnumerable<IFingerprintMatcher> matchers)
    {
        ArgumentNullException.ThrowIfNull(matchers);
        _catalogSource = catalogSource;
        _matchers = matchers.ToDictionary(m => m.Kind);
    }

    public async Task<Result<KnownItemsScan>> ExecuteAsync(DetectKnownItemsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_catalogSource.Load().TryGetValue(out var catalog, out var error))
        {
            return Result.Failure<KnownItemsScan>(error);
        }

        var found = new List<DetectedKnownItem>();
        var unresolved = new Dictionary<string, Error>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in catalog.Items)
        {
            var (matches, failure) = await MatchAsync(item, request.Profile, cancellationToken);
            if (matches.Count > 0)
            {
                found.Add(new DetectedKnownItem(item, matches));
            }
            else if (failure is not null)
            {
                unresolved[item.Id] = failure with { Message = $"Could not tell whether {item.Name} is installed: {failure.Message}" };
            }
        }

        return Result.Success(KnownItemsScan.Create(catalog, found, unresolved));
    }

    private async Task<(List<FingerprintMatch> Matches, Error? Failure)> MatchAsync(
        KnownItem item,
        RuntimeProfile profile,
        CancellationToken cancellationToken)
    {
        var matches = new List<FingerprintMatch>();
        Error? failure = null;

        // A download hash is compared at import (ADR-0018); nothing on disk is looked for.
        foreach (var fingerprint in item.Fingerprints.Where(f => f.Kind != FingerprintKind.DownloadSha256))
        {
            if (!_matchers.TryGetValue(fingerprint.Kind, out var matcher))
            {
                failure ??= new Error(DiagnosticCodes.CatalogInvalid, $"no matcher is registered for {fingerprint.Kind} fingerprints.");
                continue;
            }

            var result = await matcher.MatchAsync(fingerprint, profile, cancellationToken);
            if (result.TryGetValue(out var found, out var error))
            {
                matches.AddRange(found);
            }
            else
            {
                failure ??= error;
            }
        }

        return (matches, failure);
    }
}
