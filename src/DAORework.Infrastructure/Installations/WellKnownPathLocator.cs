using DAORework.Application.Abstractions;
using DAORework.Application.Installations;
using DAORework.Core.Common;
using DAORework.Core.Installations;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Installations;

/// <summary>Probes the usual install folders under both Program Files directories, for copies no launcher registered.</summary>
public sealed class WellKnownPathLocator : IInstallLocator
{
    private readonly InstallationProbe _probe;
    private readonly IKnownFolders _knownFolders;
    private readonly ILogger<WellKnownPathLocator> _logger;

    public WellKnownPathLocator(InstallationProbe probe, IKnownFolders knownFolders, ILogger<WellKnownPathLocator> logger)
    {
        _probe = probe;
        _knownFolders = knownFolders;
        _logger = logger;
    }

    public Task<IReadOnlyList<Installation>> LocateAsync(CancellationToken cancellationToken = default)
    {
        var installations = CandidateDirectories()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(directory => _probe.Probe(directory, notes: "Well-known folder"))
            .OfType<Installation>()
            .ToList();

        _logger.LogInformation("Well-known paths: found {Count} Dragon Age: Origins installation(s).", installations.Count);
        return Task.FromResult<IReadOnlyList<Installation>>(installations);
    }

    private IEnumerable<string> CandidateDirectories()
    {
        foreach (var relativePath in GamePaths.KnownInstallRelativePaths)
        {
            yield return Path.Combine(_knownFolders.ProgramFilesX86, relativePath);
            yield return Path.Combine(_knownFolders.ProgramFiles, relativePath);
        }
    }
}
