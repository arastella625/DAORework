namespace DAORework.Core.Preferences;

/// <summary>
/// The user's choices about the app itself, kept apart from the launch profiles (ADR-0016, ADR-0017): the app-wide look
/// and a look per profile. A look is named by id; the App owns which ids exist and falls back for one it does not know.
/// </summary>
public sealed record AppPreferences
{
    /// <summary>The schema of <c>preferences.json</c> (SDD §8.4). Bump it together with a migrator.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets the app-wide look's id, such as <c>witch-of-the-wilds</c>. Null means the default.</summary>
    public string? Look { get; init; }

    /// <summary>Gets the look of each launch profile that has its own, by profile name (ADR-0017).</summary>
    public IReadOnlyDictionary<string, string> ProfileLooks { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
