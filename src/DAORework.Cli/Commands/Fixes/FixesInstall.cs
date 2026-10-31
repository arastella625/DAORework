using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Cli.Commands.Fixes;

/// <summary>The install folder a fix command works on: the profile's.</summary>
internal static class FixesInstall
{
    public static async Task<Result<string>> ForProfileAsync(GetProfile getProfile, string profileName, CancellationToken cancellationToken)
    {
        if (!(await getProfile.ExecuteAsync(new GetProfileRequest(profileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<string>(error);
        }

        return string.IsNullOrWhiteSpace(profile.InstallDirectory)
            ? Result.Failure<string>(new Error(
                DiagnosticCodes.FixInstallNotSet,
                $"Profile '{profileName}' has no install folder.",
                $"Run: profile set-path {profileName} install <folder>"))
            : Result.Success(profile.InstallDirectory);
    }
}
