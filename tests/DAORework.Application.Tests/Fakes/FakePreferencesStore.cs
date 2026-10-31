using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Preferences;

namespace DAORework.Application.Tests.Fakes;

/// <summary>Holds the preferences in memory, or fails every load as an unreadable file does.</summary>
internal sealed class FakePreferencesStore : IPreferencesStore
{
    public AppPreferences Saved { get; set; } = new();

    public bool Unreadable { get; set; }

    public int Saves { get; private set; }

    public Task<Result<AppPreferences>> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Unreadable
            ? Result.Failure<AppPreferences>(new Error("DAO_PREFERENCES_UNREADABLE", "Unreadable."))
            : Result.Success(Saved));

    public Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default)
    {
        Saved = preferences;
        Unreadable = false;
        Saves++;
        return Task.CompletedTask;
    }
}
