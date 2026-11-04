using System.IO.Abstractions;
using DAORework.Application.Documents;
using DAORework.Core.Common;
using DAORework.Core.Documents;

namespace DAORework.Infrastructure.Installations.Steam;

/// <summary>
/// The game's Proton prefix in every Steam library (ADR-0031). The folder of the prefix's user and its Documents are taken
/// from what is on disk, since older prefixes name them differently. Only reads, and never creates a prefix.
/// </summary>
public sealed class ProtonPrefixLocator : IProtonPrefixLocator
{
    private readonly IFileSystem _fileSystem;
    private readonly SteamLibraries _libraries;

    public ProtonPrefixLocator(IFileSystem fileSystem, SteamLibraries libraries)
    {
        _fileSystem = fileSystem;
        _libraries = libraries;
    }

    public IReadOnlyList<string> FindPrefixes() =>
        [.. _libraries.Find().Select(ProtonPaths.PrefixInLibrary).Where(_fileSystem.Directory.Exists)];

    public IReadOnlyList<string> FindDocumentsFolders() =>
        [.. FindPrefixes().SelectMany(UserFolders).SelectMany(Children).Select(GamePaths.DefaultDocumentsDirectory).Where(_fileSystem.Directory.Exists)];

    private IEnumerable<string> UserFolders(string prefix) => Children(Path.Combine(prefix, "drive_c", "users"));

    private IEnumerable<string> Children(string folder) =>
        _fileSystem.Directory.Exists(folder) ? _fileSystem.Directory.EnumerateDirectories(folder) : [];
}
