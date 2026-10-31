using System.IO.Abstractions;
using DAORework.Core.Common;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Removes the folders a mod change left empty, from a removed file's folder upward. The documents folder and the
/// game's own folders are never removed, even when empty.
/// </summary>
internal static class EmptyFolders
{
    private static readonly HashSet<string> _kept = new(StringComparer.OrdinalIgnoreCase)
    {
        string.Empty, ".", "packages", "packages/core", "packages/core/override", GamePaths.AddInsDirectoryName, "Settings",
    };

    public static void PruneUpFrom(IFileSystem fileSystem, string documentsDirectory, string folder)
    {
        var root = fileSystem.Path.GetFullPath(documentsDirectory);
        while (folder.Length > root.Length
            && fileSystem.Directory.Exists(folder)
            && !_kept.Contains(fileSystem.Path.GetRelativePath(root, folder).Replace('\\', '/'))
            && !fileSystem.Directory.EnumerateFileSystemEntries(folder).Any())
        {
            fileSystem.Directory.Delete(folder);
            folder = fileSystem.Path.GetDirectoryName(folder)!;
        }
    }
}
