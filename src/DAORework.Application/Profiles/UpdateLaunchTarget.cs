using DAORework.Core.Common;
using DAORework.Core.Profiles;

namespace DAORework.Application.Profiles;

public sealed class UpdateLaunchTarget
{
    private readonly IProfileRepository _profiles;

    public UpdateLaunchTarget(IProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<Result<RuntimeProfile>> ExecuteAsync(UpdateLaunchTargetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await _profiles.LoadAsync(request.Name, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<RuntimeProfile>(ProfileErrors.NotFound(request.Name));
        }

        var updated = profile with { LaunchTarget = request.Target };
        await _profiles.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
