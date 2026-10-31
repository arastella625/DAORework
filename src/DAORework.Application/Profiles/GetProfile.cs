using DAORework.Core.Common;
using DAORework.Core.Profiles;

namespace DAORework.Application.Profiles;

public sealed class GetProfile
{
    private readonly IProfileRepository _profiles;

    public GetProfile(IProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<Result<RuntimeProfile>> ExecuteAsync(GetProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await _profiles.LoadAsync(request.Name, cancellationToken);
        return profile is null
            ? Result.Failure<RuntimeProfile>(ProfileErrors.NotFound(request.Name))
            : Result.Success(profile);
    }
}
