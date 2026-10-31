using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.App.ViewModels;

/// <summary>
/// The documents folder a mod change in the window works on: the one the profile names, as in the CLI. A change is
/// never made in a folder the profile does not name.
/// </summary>
internal static class ProfileDocuments
{
    public static async Task<Result<string>> ForAsync(GetProfile getProfile, string? profileName, CancellationToken cancellationToken)
    {
        if (profileName is null)
        {
            return Result.Failure<string>(new Error(DiagnosticCodes.ModDocumentsNotSet, "Select a profile first."));
        }

        if (!(await getProfile.ExecuteAsync(new GetProfileRequest(profileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<string>(error);
        }

        return string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory)
            ? Result.Failure<string>(new Error(
                DiagnosticCodes.ModDocumentsNotSet,
                $"Profile '{profileName}' has no documents folder.",
                "Choose it on the Profile page: Detect, then Use the documents folder."))
            : Result.Success(profile.UserDocumentsDirectory);
    }
}
