using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Profiles;

namespace DAORework.Application.Installations;

/// <summary>Points a profile at a detected install in one step: the install directory and whichever executables were found.</summary>
public sealed class ApplyInstallation
{
    private readonly IProfileRepository _profiles;

    public ApplyInstallation(IProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<Result<RuntimeProfile>> ExecuteAsync(ApplyInstallationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Installation);

        var profile = await _profiles.LoadAsync(request.ProfileName, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<RuntimeProfile>(ProfileErrors.NotFound(request.ProfileName));
        }

        var installation = request.Installation;
        var updated = profile with
        {
            InstallDirectory = installation.InstallDirectory,
            GameExecutablePath = installation.GameExecutablePath ?? profile.GameExecutablePath,
            LauncherExecutablePath = installation.LauncherExecutablePath ?? profile.LauncherExecutablePath,
        };

        await _profiles.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
