using System.IO.Abstractions;
using DAORework.Application.Installations;
using DAORework.Core.Installations;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Installations.Steam;

/// <summary>
/// Finds the Steam copy in any library <see cref="SteamLibraries"/> knows: in each one, the app manifest for Dragon Age:
/// Origins (app 47810) or the usual folder names.
/// </summary>
public sealed class SteamLibraryLocator : IInstallLocator
{
    /// <summary>Dragon Age: Origins and its Ultimate Edition share this Steam app id.</summary>
    public const string AppId = "47810";

    private static readonly string[] _fallbackFolderNames = ["Dragon Age Ultimate Edition", "Dragon Age Origins"];

    private readonly IFileSystem _fileSystem;
    private readonly SteamLibraries _libraries;
    private readonly InstallationProbe _probe;
    private readonly ILogger<SteamLibraryLocator> _logger;

    public SteamLibraryLocator(
        IFileSystem fileSystem,
        SteamLibraries libraries,
        InstallationProbe probe,
        ILogger<SteamLibraryLocator> logger)
    {
        _fileSystem = fileSystem;
        _libraries = libraries;
        _probe = probe;
        _logger = logger;
    }

    public Task<IReadOnlyList<Installation>> LocateAsync(CancellationToken cancellationToken = default)
    {
        var installations = new List<Installation>();
        foreach (var library in _libraries.Find())
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
