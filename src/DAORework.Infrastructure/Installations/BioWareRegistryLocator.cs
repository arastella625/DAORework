using DAORework.Application.Abstractions;
using DAORework.Application.Installations;
using DAORework.Core.Installations;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Installations;

/// <summary>
/// Finds the copy registered under <c>BioWare\Dragon Age</c>, which the EA App, Origin and the retail installer write.
/// The distribution is read off the path; a path that says nothing is taken as the EA App.
/// </summary>
public sealed class BioWareRegistryLocator : IInstallLocator
{
    private static readonly string[] _keys = [@"SOFTWARE\WOW6432Node\BioWare\Dragon Age", @"SOFTWARE\BioWare\Dragon Age"];

    private readonly IRegistry _registry;
    private readonly InstallationProbe _probe;
    private readonly ILogger<BioWareRegistryLocator> _logger;

    public BioWareRegistryLocator(IRegistry registry, InstallationProbe probe, ILogger<BioWareRegistryLocator> logger)
    {
        _registry = registry;
        _probe = probe;
        _logger = logger;
    }

    public Task<IReadOnlyList<Installation>> LocateAsync(CancellationToken cancellationToken = default)
    {
        var installations = _keys
            .Select(key => _registry.GetStringValue(RegistryRoot.LocalMachine, key, "Path"))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => _probe.Probe(path!, DistributionOf(path!), "BioWare registry key"))
            .OfType<Installation>()
            .ToList();

        _logger.LogInformation("BioWare registry key: found {Count} Dragon Age: Origins installation(s).", installations.Count);
        return Task.FromResult<IReadOnlyList<Installation>>(installations);
    }

    private static Distribution DistributionOf(string path)
    {
        var guessed = InstallationProbe.GuessDistribution(path);
        return guessed == Distribution.Unknown ? Distribution.EaApp : guessed;
    }
}
