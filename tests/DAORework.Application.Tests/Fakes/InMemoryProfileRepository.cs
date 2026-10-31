using DAORework.Application.Profiles;
using DAORework.Core.Profiles;

namespace DAORework.Application.Tests.Fakes;

internal sealed class InMemoryProfileRepository : IProfileRepository
{
    public Dictionary<string, RuntimeProfile> Profiles { get; } = new(StringComparer.Ordinal);

    public int SaveCount { get; private set; }

    public Task<RuntimeProfile?> LoadAsync(string profileName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Profiles.GetValueOrDefault(profileName));

    public Task SaveAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        Profiles[profile.Name] = profile;
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Profiles.Keys.Order().ToList());
}
