using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Installations;

namespace DAORework.Application.Installations;

/// <summary>Turns a candidate directory into an <see cref="Installation"/> by checking which executables exist there. Shared by every locator.</summary>
public sealed class InstallationProbe
{
    private readonly IFileSystem _fileSystem;

    public InstallationProbe(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <returns>The installation, or null when neither executable exists under <paramref name="directory"/>.</returns>
    public Installation? Probe(string directory, Distribution? distribution = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var root = directory.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (root.Length == 0)
        {
            return null;
        }

        var gameExecutable = GamePaths.GameExecutable(root);
        var launcherExecutable = GamePaths.LauncherExecutable(root);
        var installation = new Installation
        {
            InstallDirectory = root,
            GameExecutablePath = _fileSystem.File.Exists(gameExecutable) ? gameExecutable : null,
            LauncherExecutablePath = _fileSystem.File.Exists(launcherExecutable) ? launcherExecutable : null,
            Distribution = distribution ?? GuessDistribution(root),
            Notes = notes,
        };

        return installation.IsValid ? installation : null;
    }

    /// <summary>Reads the distribution off the folder name when no source said which it is.</summary>
    public static Distribution GuessDistribution(string installDirectory)
    {
        ArgumentNullException.ThrowIfNull(installDirectory);
        if (installDirectory.Contains("steamapps", StringComparison.OrdinalIgnoreCase))
        {
            return Distribution.Steam;
        }

        if (installDirectory.Contains("GOG", StringComparison.OrdinalIgnoreCase))
        {
            return Distribution.Gog;
        }

        if (installDirectory.Contains("EA Games", StringComparison.OrdinalIgnoreCase) ||
            installDirectory.Contains("Origin Games", StringComparison.OrdinalIgnoreCase))
        {
            return Distribution.EaApp;
        }

        return Distribution.Unknown;
    }
}
