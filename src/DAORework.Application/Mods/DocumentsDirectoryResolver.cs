using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Profiles;

namespace DAORework.Application.Mods;

/// <summary>The documents directory a profile points at, or the game's default under My Documents.</summary>
internal static class DocumentsDirectoryResolver
{
    /// <returns>The folder; empty, never a relative path, when the profile names none and the platform reports no Documents folder.</returns>
    public static string Resolve(RuntimeProfile profile, IKnownFolders knownFolders)
    {
        if (!string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory))
        {
            return profile.UserDocumentsDirectory;
        }

        return string.IsNullOrWhiteSpace(knownFolders.MyDocuments) ? string.Empty : GamePaths.DefaultDocumentsDirectory(knownFolders.MyDocuments);
    }
}
