using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Preferences;

namespace DAORework.Application.Looks;

/// <summary>
/// Deletes a custom look and its picture. The app-wide look and any profile that used it go back to the default
/// (ADR-0017), so nothing is left pointing at a look that is gone.
/// </summary>
public sealed class DeleteLook
{
    private readonly ILookStore _looks;
    private readonly IPreferencesStore _preferences;

    public DeleteLook(ILookStore looks, IPreferencesStore preferences)
    {
        _looks = looks;
        _preferences = preferences;
    }

    public async Task<Result<AppPreferences>> ExecuteAsync(DeleteLookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!(await _looks.LoadAsync(request.LookId, cancellationToken)).TryGetValue(out _, out var error)
            && error.Code == DiagnosticCodes.LookNotFound)
        {
            return Result.Failure<AppPreferences>(error);
        }

        await _looks.DeleteAsync(request.LookId, cancellationToken); // An unreadable look can still be deleted.
        var loaded = await _preferences.LoadAsync(cancellationToken);
        var preferences = loaded.TryGetValue(out var current, out _) ? current : new AppPreferences();
        var kept = preferences.ProfileLooks.Where(entry => entry.Value != request.LookId).ToDictionary(StringComparer.OrdinalIgnoreCase);
        var updated = preferences with { Look = preferences.Look == request.LookId ? null : preferences.Look, ProfileLooks = kept };
        if (updated.Look != preferences.Look || kept.Count != preferences.ProfileLooks.Count)
        {
            await _preferences.SaveAsync(updated, cancellationToken);
        }

        return Result.Success(updated);
    }
}
