using DAORework.Core.Common;
using DAORework.Core.Installations;

namespace DAORework.Application.Installations;

/// <summary>Merges every <see cref="IInstallLocator"/> and the manual paths: distinct by directory, installs with a game executable first (Composite, SDD §5).</summary>
public sealed class DetectInstallations
{
    private readonly IReadOnlyList<IInstallLocator> _locators;
    private readonly InstallationProbe _probe;

    public DetectInstallations(IEnumerable<IInstallLocator> locators, InstallationProbe probe)
    {
        _locators = locators.ToList();
        _probe = probe;
    }

    public async Task<Result<IReadOnlyList<Installation>>> ExecuteAsync(DetectInstallationsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var found = new List<Installation>();
        foreach (var locator in _locators)
        {
            cancellationToken.ThrowIfCancellationRequested();
            found.AddRange(await locator.LocateAsync(cancellationToken));
        }

        // Manual paths come last, so a folder a launcher also knows keeps that launcher's label.
        foreach (var path in request.ManualPaths ?? [])
        {
            if (!string.IsNullOrWhiteSpace(path) && _probe.Probe(path, Distribution.Manual) is { } manual)
            {
                found.Add(manual);
            }
        }

        IReadOnlyList<Installation> installations = found
            .Where(i => i.IsValid)
            .GroupBy(i => i.InstallDirectory, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(i => i.HasValidGameExecutable)
            .ThenBy(i => i.Distribution)
            .ToList();

        return Result.Success(installations);
    }
}
