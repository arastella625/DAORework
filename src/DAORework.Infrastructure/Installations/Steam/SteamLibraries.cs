using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Installations.Steam;

/// <summary>
/// Every Steam library on the machine (ADR-0031): the Steam folders that exist, wherever they are, and every library
/// their <c>steamapps/libraryfolders.vdf</c> names, on any drive or mount. On Windows that is the folder the registry
/// names, or Program Files. On Linux it is the native, Flatpak and Snap folders under the home folder. A folder reached
/// through a link counts once.
/// </summary>
public sealed class SteamLibraries
{
    private static readonly string[][] _homeRoots =
    [
        [".local", "share", "Steam"],
        [".steam", "steam"],
        [".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"],
        [".var", "app", "com.valvesoftware.Steam", "data", "Steam"],
        ["snap", "steam", "common", ".local", "share", "Steam"],
    ];

    private readonly IFileSystem _fileSystem;
    private readonly IRegistry _registry;
    private readonly IKnownFolders _knownFolders;
    private readonly ILogger<SteamLibraries> _logger;

    public SteamLibraries(IFileSystem fileSystem, IRegistry registry, IKnownFolders knownFolders, ILogger<SteamLibraries> logger)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _knownFolders = knownFolders;
        _logger = logger;
    }

    public IReadOnlyList<string> Find()
    {
        var libraries = new List<string>();
        foreach (var root in Roots())
        {
            libraries.Add(root);
            var manifest = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (_fileSystem.File.Exists(manifest))
            {
                libraries.AddRange(ReadLibraryPaths(manifest));
            }
        }

        return [.. libraries.Select(Resolved).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private List<string> Roots()
    {
        var roots = new List<string>();
        var fromRegistry = _registry.GetStringValue(RegistryRoot.CurrentUser, @"Software\Valve\Steam", "SteamPath")
            ?? _registry.GetStringValue(RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
        if (!string.IsNullOrWhiteSpace(fromRegistry))
        {
            roots.Add(fromRegistry.Replace('/', Path.DirectorySeparatorChar));
        }
        else if (!string.IsNullOrWhiteSpace(_knownFolders.ProgramFilesX86))
        {
            roots.Add(Path.Combine(_knownFolders.ProgramFilesX86, "Steam"));
        }

        if (!string.IsNullOrWhiteSpace(_knownFolders.UserProfile))
        {
            roots.AddRange(_homeRoots.Select(parts => Path.Combine([_knownFolders.UserProfile, .. parts])).Where(_fileSystem.Directory.Exists));
        }

        return roots;
    }

    /// <summary>The folder a link leads to, so <c>~/.steam/steam</c> and the folder it points at are one library.</summary>
    private string Resolved(string folder)
    {
        try
        {
            var info = _fileSystem.DirectoryInfo.New(folder);
            return info.Exists && info.LinkTarget is not null ? info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? folder : folder;
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Could not follow the link at {Path}.", folder);
            return folder;
        }
    }

    private IEnumerable<string> ReadLibraryPaths(string manifestPath)
    {
        VdfNode document;
        try
        {
            document = VdfReader.Parse(_fileSystem.File.ReadAllText(manifestPath));
        }
        catch (InvalidDataException exception)
        {
            _logger.LogWarning(exception, "Could not read the Steam library list at {Path}.", manifestPath);
            return [];
        }

        var folders = document.GetChild("libraryfolders");
        return folders is null
            ? []
            : folders.Children.Values.Select(f => f.GetString("path")).OfType<string>().Select(p => p.Replace('/', Path.DirectorySeparatorChar));
    }
}
