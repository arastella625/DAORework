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
    public static async Task<Result<string>> ForAsync(GetProfile getProfile, string? profileName, CancellationToken cancellationToken) =>
        (await FoldersAsync(getProfile, profileName, cancellationToken)).TryGetValue(out var folders, out var error)
            ? Result.Success(folders.Documents)
            : Result.Failure<string>(error);

    /// <summary>The documents folder, and the install folder a plan's warnings look at (P2.5-22).</summary>
    public static async Task<Result<(string Documents, string? Install)>> FoldersAsync(GetProfile getProfile, string? profileName, CancellationToken cancellationToken)
    {
        if (profileName is null)
        {
            return Result.Failure<(string, string?)>(new Error(DiagnosticCodes.ModDocumentsNotSet, "Select a profile first."));
        }

        if (!(await getProfile.ExecuteAsync(new GetProfileRequest(profileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<(string, string?)>(error);
        }

        return string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory)
            ? Result.Failure<(string, string?)>(new Error(
                DiagnosticCodes.ModDocumentsNotSet,
                $"Profile '{profileName}' has no documents folder.",
                "Choose it on the Profile page: Detect, then Use the documents folder."))
            : Result.Success<(string, string?)>((profile.UserDocumentsDirectory, profile.InstallDirectory));
    }
}
