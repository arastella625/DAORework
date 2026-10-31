using DAORework.Application.Abstractions;
using DAORework.Application.Installations;
using DAORework.Core.Installations;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.Installations;

/// <summary>Finds the GOG copy through the keys GOG Galaxy writes: one sub-key per game under <c>GOG.com\Games</c> with <c>gameName</c> and <c>path</c>.</summary>
public sealed class GogRegistryLocator : IInstallLocator
{
    private static readonly string[] _gamesKeys = [@"SOFTWARE\WOW6432Node\GOG.com\Games", @"SOFTWARE\GOG.com\Games"];

    private readonly IRegistry _registry;
    private readonly InstallationProbe _probe;
    private readonly ILogger<GogRegistryLocator> _logger;

    public GogRegistryLocator(IRegistry registry, InstallationProbe probe, ILogger<GogRegistryLocator> logger)
    {
        _registry = registry;
        _probe = probe;
        _logger = logger;
    }

    public Task<IReadOnlyList<Installation>> LocateAsync(CancellationToken cancellationToken = default)
    {
        var installations = _gamesKeys
            .SelectMany(GamePaths)
            .Select(path => _probe.Probe(path, Distribution.Gog, "GOG Galaxy registry"))
            .OfType<Installation>()
            .ToList();

        _logger.LogInformation("GOG registry: found {Count} Dragon Age: Origins installation(s).", installations.Count);
        return Task.FromResult<IReadOnlyList<Installation>>(installations);
    }

    private IEnumerable<string> GamePaths(string gamesKey)
    {
        foreach (var gameId in _registry.GetSubKeyNames(RegistryRoot.LocalMachine, gamesKey))
        {
            var gameKey = $@"{gamesKey}\{gameId}";
            var name = _registry.GetStringValue(RegistryRoot.LocalMachine, gameKey, "gameName") ?? string.Empty;
            var path = _registry.GetStringValue(RegistryRoot.LocalMachine, gameKey, "path");
            if (IsDragonAgeOrigins(name) && !string.IsNullOrWhiteSpace(path))
            {
                yield return path;
            }
        }
    }

    private static bool IsDragonAgeOrigins(string gameName) =>
        gameName.Contains("Dragon Age", StringComparison.OrdinalIgnoreCase) &&
        gameName.Contains("Origins", StringComparison.OrdinalIgnoreCase);
}
