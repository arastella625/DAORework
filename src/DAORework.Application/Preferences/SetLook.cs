using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Preferences;

namespace DAORework.Application.Preferences;

/// <summary>
/// Saves the app-wide look the user picked. A preferences file that cannot be read is replaced by one that holds the
/// pick (ADR-0016): what it held is already lost, and the pick is what the user wants now.
/// </summary>
public sealed class SetLook
{
    private readonly IPreferencesStore _store;

    public SetLook(IPreferencesStore store)
    {
        _store = store;
    }

    public async Task<Result<AppPreferences>> ExecuteAsync(SetLookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LookId);

        var loaded = await _store.LoadAsync(cancellationToken);
        var current = loaded.TryGetValue(out var preferences, out _) ? preferences : new AppPreferences();
        var updated = current with { Look = request.LookId };
        await _store.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
