using DAORework.Core.Common;

namespace DAORework.Application.Profiles;

public sealed class ListProfiles
{
    private readonly IProfileRepository _profiles;

    public ListProfiles(IProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(ListProfilesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var names = await _profiles.ListNamesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<string>>(names.Order(StringComparer.OrdinalIgnoreCase).ToList());
    }
}
