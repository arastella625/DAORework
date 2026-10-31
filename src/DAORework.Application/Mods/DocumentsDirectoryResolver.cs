using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Profiles;

namespace DAORework.Application.Mods;

/// <summary>The documents directory a profile points at, or the game's default under My Documents.</summary>
internal static class DocumentsDirectoryResolver
{
    public static string Resolve(RuntimeProfile profile, IKnownFolders knownFolders) =>
        !string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory)
            ? profile.UserDocumentsDirectory
            : GamePaths.DefaultDocumentsDirectory(knownFolders.MyDocuments);
}
