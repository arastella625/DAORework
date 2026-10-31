using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Abstractions;

/// <summary>The custom looks in app-data, one folder each: <c>looks\{lookId}\look.json</c> and its picture (ADR-0017).</summary>
public interface ILookStore
{
    /// <summary>Every look that can be read, and an error for each one that cannot, so one bad file never hides the rest.</summary>
    Task<LookListing> ListAsync(CancellationToken cancellationToken = default);

    /// <returns>The look, or <c>DAO_LOOK_NOT_FOUND</c> or <c>DAO_LOOK_UNREADABLE</c>.</returns>
    Task<Result<Look>> LoadAsync(string lookId, CancellationToken cancellationToken = default);

    /// <summary>Saves the look's <c>look.json</c> atomically, creating its folder.</summary>
    Task SaveAsync(Look look, CancellationToken cancellationToken = default);

    /// <summary>Removes the look's folder, its picture included. A look that is not there is already deleted.</summary>
    Task DeleteAsync(string lookId, CancellationToken cancellationToken = default);
}
