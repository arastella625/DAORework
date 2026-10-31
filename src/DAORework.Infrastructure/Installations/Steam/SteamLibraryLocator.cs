using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Installations;
using DAORework.Core.Installations;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Installations.Steam;

/// <summary>
/// Finds the Steam copy on any library drive: the Steam folder from the registry (or Program Files), every library in
/// <c>steamapps/libraryfolders.vdf</c>, and in each one the app manifest for Dragon Age: Origins (app 47810) or the
/// usual folder names.
/// </summary>
public sealed class SteamLibraryLocator : IInstallLocator
{
    /// <summary>Dragon Age: Origins and its Ultimate Edition share this Steam app id.</summary>
    public const string AppId = "47810";

    private static readonly string[] _fallbackFolderNames = ["Dragon Age Ultimate Edition", "Dragon Age Origins"];

    private readonly IFileSystem _fileSystem;
    private readonly IRegistry _registry;
    private readonly IKnownFolders _knownFolders;
    private readonly InstallationProbe _probe;
    private readonly ILogger<SteamLibraryLocator> _logger;

    public SteamLibraryLocator(
        IFileSystem fileSystem,
        IRegistry registry,
        IKnownFolders knownFolders,
        InstallationProbe probe,
        ILogger<SteamLibraryLocator> logger)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _knownFolders = knownFolders;
        _probe = probe;
        _logger = logger;
    }

    public Task<IReadOnlyList<Installation>> LocateAsync(CancellationToken cancellationToken = default)
    {
        var installations = new List<Installation>();
        foreach (var library in Libraries())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FindInLibrary(library) is { } installation)
            {
                installations.Add(installation);
            }
        }

        _logger.LogInformation("Steam libraries: found {Count} Dragon Age: Origins installation(s).", installations.Count);
        return Task.FromResult<IReadOnlyList<Installation>>(installations);
    }

    private IEnumerable<string> Libraries()
    {
        var steamRoot = SteamRoot();
        var libraries = new List<string> { steamRoot };

        var manifest = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (_fileSystem.File.Exists(manifest))
        {
            libraries.AddRange(ReadLibraryPaths(manifest));
        }

        return libraries.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private string SteamRoot()
    {
        var fromRegistry = _registry.GetStringValue(RegistryRoot.CurrentUser, @"Software\Valve\Steam", "SteamPath")
            ?? _registry.GetStringValue(RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
        return string.IsNullOrWhiteSpace(fromRegistry)
            ? Path.Combine(_knownFolders.ProgramFilesX86, "Steam")
            : fromRegistry.Replace('/', Path.DirectorySeparatorChar);
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

    private Installation? FindInLibrary(string library)
    {
        var steamApps = Path.Combine(library, "steamapps");
        var installDir = ReadInstallDir(Path.Combine(steamApps, $"appmanifest_{AppId}.acf"));
        var candidates = installDir is null ? _fallbackFolderNames : [installDir];

        return candidates
            .Select(name => _probe.Probe(Path.Combine(steamApps, "common", name), Distribution.Steam, $"Steam library {library}"))
            .FirstOrDefault(i => i is not null);
    }

    private string? ReadInstallDir(string appManifestPath)
    {
        if (!_fileSystem.File.Exists(appManifestPath))
        {
            return null;
        }

        try
        {
            return VdfReader.Parse(_fileSystem.File.ReadAllText(appManifestPath)).GetChild("AppState")?.GetString("installdir");
        }
        catch (InvalidDataException exception)
        {
            _logger.LogWarning(exception, "Could not read the Steam app manifest at {Path}.", appManifestPath);
            return null;
        }
    }
}
