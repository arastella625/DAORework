namespace DAORework.Core.Looks;

/// <summary>
/// A custom look (ADR-0017): a base theme, an optional accent and an optional background picture, under a name the user
/// gives. The four built-in themes are looks too, but they are the App's and are never stored.
/// </summary>
public sealed record Look
{
    /// <summary>The schema of <c>look.json</c> (SDD §8.4). Bump it together with a migrator.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Every custom look's id starts with this, so it never collides with a built-in theme's id.</summary>
    public const string IdPrefix = "look-";

    public const int MaxNameLength = 40;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Gets the id of the built-in theme this look starts from, such as <c>witch-of-the-wilds</c>.</summary>
    public required string BaseTheme { get; init; }

    /// <summary>Gets the accent pick as <c>#RRGGBB</c>, or null to keep the base theme's accent.</summary>
    public string? Accent { get; init; }

    public LookBackground? Background { get; init; }
}
