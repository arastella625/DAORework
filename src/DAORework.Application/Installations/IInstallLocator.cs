using DAORework.Core.Installations;

namespace DAORework.Application.Installations;

/// <summary>One source of install candidates (well-known paths, and from P1-01 Steam, GOG, EA App). Returns only installs it found valid.</summary>
public interface IInstallLocator
{
    Task<IReadOnlyList<Installation>> LocateAsync(CancellationToken cancellationToken = default);
}
