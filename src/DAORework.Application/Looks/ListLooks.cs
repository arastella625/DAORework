using DAORework.Application.Abstractions;
using DAORework.Core.Common;

namespace DAORework.Application.Looks;

/// <summary>Lists the custom looks by name, with any that could not be read.</summary>
public sealed class ListLooks
{
    private readonly ILookStore _looks;

    public ListLooks(ILookStore looks)
    {
        _looks = looks;
    }

    public async Task<Result<LookListing>> ExecuteAsync(ListLooksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var listing = await _looks.ListAsync(cancellationToken);
        return Result.Success(listing with { Looks = listing.Looks.OrderBy(look => look.Name, StringComparer.CurrentCultureIgnoreCase).ToList() });
    }
}
