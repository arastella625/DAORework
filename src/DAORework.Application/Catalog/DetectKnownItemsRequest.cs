using DAORework.Core.Profiles;

namespace DAORework.Application.Catalog;

/// <param name="Profile">The profile whose game is checked.</param>
/// <param name="Only">The ids of the entries to check, such as what a mod needs; every entry when null.</param>
public sealed record DetectKnownItemsRequest(RuntimeProfile Profile, IReadOnlySet<string>? Only = null);
