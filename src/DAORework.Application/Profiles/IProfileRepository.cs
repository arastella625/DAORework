using DAORework.Core.Profiles;

namespace DAORework.Application.Profiles;

public interface IProfileRepository
{
    Task<RuntimeProfile?> LoadAsync(string profileName, CancellationToken cancellationToken = default);

    Task SaveAsync(RuntimeProfile profile, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default);
}
