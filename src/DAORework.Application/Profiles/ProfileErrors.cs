using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Application.Profiles;

internal static class ProfileErrors
{
    public static Error NotFound(string profileName) =>
        new(DiagnosticCodes.ProfileNotFound, $"Profile '{profileName}' was not found.", $"Run: init {profileName}");
}
