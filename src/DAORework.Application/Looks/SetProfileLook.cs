using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Preferences;

namespace DAORework.Application.Looks;

/// <summary>
/// Sets the look a launch profile shows (ADR-0017, chosen by the owner on 2026-10-29). The look lives in
/// <c>preferences.json</c>, not in the profile, so the launch settings stay free of presentation.
/// </summary>
public sealed class SetProfileLook
{
    private readonly IPreferencesStore _preferences;

    public SetProfileLook(IPreferencesStore preferences)
    {
        _preferences = preferences;
    }

    public async Task<Result<AppPreferences>> ExecuteAsync(SetProfileLookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProfileName);

        var loaded = await _preferences.LoadAsync(cancellationToken);
        var preferences = loaded.TryGetValue(out var current, out _) ? current : new AppPreferences();
        var looks = new Dictionary<string, string>(preferences.ProfileLooks, StringComparer.OrdinalIgnoreCase);
        if (request.LookId is null)
        {
            looks.Remove(request.ProfileName);
        }
        else
        {
            looks[request.ProfileName] = request.LookId;
        }

        var updated = preferences with { ProfileLooks = looks };
        await _preferences.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
