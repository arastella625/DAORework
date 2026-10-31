namespace DAORework.Core.Mods;

public sealed record DaoModProfile
{
    /// <summary>The schema of the persisted document (SDD §8.4). Bump it together with a migrator.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Name { get; init; } = "Vanilla";

    public string? Description { get; init; }

    public IReadOnlyList<string> EnabledOverrideDirectories { get; init; } = [];

    public IReadOnlyList<string> EnabledPackagePaths { get; init; } = [];
}
