using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Cli.Commands.Mods;

/// <summary>The documents folder a mod command works on: the profile's.</summary>
internal static class ModsDocuments
{
    public static async Task<Result<string>> ForProfileAsync(GetProfile getProfile, string profileName, CancellationToken cancellationToken) =>
        (await FoldersAsync(getProfile, profileName, cancellationToken)).TryGetValue(out var folders, out var error)
            ? Result.Success(folders.Documents)
            : Result.Failure<string>(error);

    /// <summary>The documents folder, and the install folder a plan's warnings look at (P2.5-22).</summary>
    public static async Task<Result<(string Documents, string? Install)>> FoldersAsync(GetProfile getProfile, string profileName, CancellationToken cancellationToken)
    {
        if (!(await getProfile.ExecuteAsync(new GetProfileRequest(profileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<(string, string?)>(error);
        }

        return string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory)
            ? Result.Failure<(string, string?)>(new Error(
                DiagnosticCodes.ModDocumentsNotSet,
                $"Profile '{profileName}' has no documents folder.",
                $"Run: profile set-path {profileName} documents <folder>"))
            : Result.Success<(string, string?)>((profile.UserDocumentsDirectory, profile.InstallDirectory));
    }
}
