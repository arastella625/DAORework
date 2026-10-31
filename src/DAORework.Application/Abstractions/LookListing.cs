using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Abstractions;

public sealed record LookListing(IReadOnlyList<Look> Looks, IReadOnlyList<Error> Unreadable);
