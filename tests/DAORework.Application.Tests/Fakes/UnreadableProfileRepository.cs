using DAORework.Application.Profiles;
using DAORework.Core.Profiles;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Lists one more profile than <paramref name="inner"/> has, and fails to load it, as a damaged profile file does.</summary>
internal sealed class UnreadableProfileRepository(IProfileRepository inner, string unreadableName, Exception failure) : IProfileRepository
{
    public Task<RuntimeProfile?> LoadAsync(string profileName, CancellationToken cancellationToken = default) =>
        profileName == unreadableName ? throw failure : inner.LoadAsync(profileName, cancellationToken);

    public Task SaveAsync(RuntimeProfile profile, CancellationToken cancellationToken = default) => inner.SaveAsync(profile, cancellationToken);

    public async Task<IReadOnlyList<string>> ListNamesAsync(CancellationToken cancellationToken = default) =>
        [unreadableName, .. await inner.ListNamesAsync(cancellationToken)];
}
