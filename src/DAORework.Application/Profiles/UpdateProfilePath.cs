using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Profiles;

/// <summary>Sets one of a profile's paths. The path is stored as given; whether it exists is the diagnostics' job.</summary>
public sealed class UpdateProfilePath
{
    private readonly IProfileRepository _profiles;

    public UpdateProfilePath(IProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<Result<RuntimeProfile>> ExecuteAsync(UpdateProfilePathRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return Result.Failure<RuntimeProfile>(new Error(DiagnosticCodes.ProfilePathInvalid, "The path is empty."));
        }

        var profile = await _profiles.LoadAsync(request.Name, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<RuntimeProfile>(ProfileErrors.NotFound(request.Name));
        }

        var path = request.Path.Trim();
        var updated = request.Kind switch
        {
            ProfilePathKind.Install => profile with { InstallDirectory = path },
            ProfilePathKind.Game => profile with { GameExecutablePath = path },
            ProfilePathKind.Launcher => profile with { LauncherExecutablePath = path },
            ProfilePathKind.Documents => profile with { UserDocumentsDirectory = path },
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Unknown path kind."),
        };

        await _profiles.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
