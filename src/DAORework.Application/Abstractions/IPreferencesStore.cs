using DAORework.Core.Common;
using DAORework.Core.Preferences;

namespace DAORework.Application.Abstractions;

/// <summary>The app's own preferences, in <c>preferences.json</c> in the app-data folder (ADR-0016, ADR-0017).</summary>
public interface IPreferencesStore
{
    /// <returns>The saved preferences, the defaults when none are saved, or <c>DAO_PREFERENCES_UNREADABLE</c>.</returns>
    Task<Result<AppPreferences>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the saved preferences atomically, so a crash never leaves half a file.</summary>
    Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default);
}
