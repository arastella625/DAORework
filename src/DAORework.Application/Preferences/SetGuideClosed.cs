using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Preferences;

namespace DAORework.Application.Preferences;

/// <summary>
/// Saves whether the cozy guide is closed: skipped or finished, or opened again from Settings (ADR-0051 §3, §6). Like
/// <see cref="SetLook"/>, a preferences file that can't be read is replaced by one that holds the choice.
/// </summary>
public sealed class SetGuideClosed(IPreferencesStore store)
{
    public async Task<Result<AppPreferences>> ExecuteAsync(bool closed, CancellationToken cancellationToken = default)
    {
        var loaded = await store.LoadAsync(cancellationToken);
        var updated = (loaded.TryGetValue(out var preferences, out _) ? preferences : new AppPreferences()) with { GuideClosed = closed };
        await store.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
