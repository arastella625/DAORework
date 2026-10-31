using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Profiles;

namespace DAORework.Application.Catalog;

/// <summary>
/// Looks for one kind of fingerprint on disk (Strategy, ADR-0011). Returns every place it matched, an empty list when
/// it is not there, and a failure only when it could not tell, for example an unreadable file.
/// </summary>
public interface IFingerprintMatcher
{
    FingerprintKind Kind { get; }

    Task<Result<IReadOnlyList<FingerprintMatch>>> MatchAsync(
        KnownItemFingerprint fingerprint,
        RuntimeProfile profile,
        CancellationToken cancellationToken = default);
}
