using System.IO.Abstractions;

namespace DAORework.Infrastructure.Mods;

/// <summary>Turns a documents-relative path with <c>/</c> separators into a full path, refusing any that would leave the folder.</summary>
internal static class DocumentsPaths
{
    public static string Resolve(IFileSystem fileSystem, string documentsDirectory, string relativePath)
    {
        var root = fileSystem.Path.GetFullPath(documentsDirectory);
        var full = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(root, relativePath.Replace('/', fileSystem.Path.DirectorySeparatorChar)));
        var prefix = root.EndsWith(fileSystem.Path.DirectorySeparatorChar) ? root : root + fileSystem.Path.DirectorySeparatorChar;
        return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? full
            : throw new InvalidOperationException($"Refusing '{relativePath}': it is outside the documents folder {documentsDirectory}.");
    }
}
