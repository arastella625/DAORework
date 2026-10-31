using System.IO.Abstractions;
using DAORework.Infrastructure.Installations.Steam;

namespace DAORework.Infrastructure.Launching;

/// <summary>
/// Tells whether an install folder is Steam's copy of Dragon Age: Origins (ADR-0022): it is
/// <c>&lt;library&gt;/steamapps/common/&lt;folder&gt;</c>, and the library's <c>appmanifest_47810.acf</c> names that folder.
/// </summary>
public sealed class SteamGameInstall
{
    private readonly IFileSystem _fileSystem;

    public SteamGameInstall(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public bool IsSteamInstall(string? installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return false;
        }

        var folder = _fileSystem.DirectoryInfo.New(Path.TrimEndingDirectorySeparator(installDirectory));
        var common = folder.Parent;
        var steamApps = common?.Parent;
        if (common is null || steamApps is null
            || !common.Name.Equals("common", StringComparison.OrdinalIgnoreCase)
            || !steamApps.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var manifest = Path.Combine(steamApps.FullName, $"appmanifest_{SteamLibraryLocator.AppId}.acf");
        return _fileSystem.File.Exists(manifest)
            && string.Equals(ReadInstallDir(manifest), folder.Name, StringComparison.OrdinalIgnoreCase);
    }

    private string? ReadInstallDir(string manifestPath)
    {
        try
        {
            return VdfReader.Parse(_fileSystem.File.ReadAllText(manifestPath)).GetChild("AppState")?.GetString("installdir");
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
