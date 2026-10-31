using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Cli.Commands.Mods;

/// <summary>The documents folder a mod command works on: the profile's.</summary>
internal static class ModsDocuments
{
    public static async Task<Result<string>> ForProfileAsync(GetProfile getProfile, string profileName, CancellationToken cancellationToken)
    {
        if (!(await getProfile.ExecuteAsync(new GetProfileRequest(profileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<string>(error);
        }

        return string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory)
            ? Result.Failure<string>(new Error(
                DiagnosticCodes.ModDocumentsNotSet,
                $"Profile '{profileName}' has no documents folder.",
                $"Run: profile set-path {profileName} documents <folder>"))
            : Result.Success(profile.UserDocumentsDirectory);
    }
}
