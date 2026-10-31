using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Preferences;

namespace DAORework.Application.Preferences;

/// <summary>Reads the app's preferences. None saved yet is not a failure: it is the defaults.</summary>
public sealed class GetPreferences
{
    private readonly IPreferencesStore _store;

    public GetPreferences(IPreferencesStore store)
    {
        _store = store;
    }

    public Task<Result<AppPreferences>> ExecuteAsync(GetPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _store.LoadAsync(cancellationToken);
    }
}
