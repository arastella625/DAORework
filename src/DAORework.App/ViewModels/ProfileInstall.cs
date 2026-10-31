using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.App.ViewModels;

/// <summary>The install folder a fix in the window works on: the one the profile names, as in the CLI (ADR-0013).</summary>
internal static class ProfileInstall
{
    public static async Task<Result<string>> ForAsync(GetProfile getProfile, string? profileName, CancellationToken cancellationToken)
    {
        if (profileName is null)
        {
            return Result.Failure<string>(new Error(DiagnosticCodes.FixInstallNotSet, "Select a profile first."));
        }

        if (!(await getProfile.ExecuteAsync(new GetProfileRequest(profileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<string>(error);
        }

        return string.IsNullOrWhiteSpace(profile.InstallDirectory)
            ? Result.Failure<string>(new Error(
                DiagnosticCodes.FixInstallNotSet,
                $"Profile '{profileName}' has no install folder.",
                "Choose it on the Profile page: Detect, then Use the install."))
            : Result.Success(profile.InstallDirectory);
    }
}
