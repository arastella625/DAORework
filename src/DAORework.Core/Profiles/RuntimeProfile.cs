using DAORework.Core.Launching;

namespace DAORework.Core.Profiles;

/// <summary>A named launch configuration. Changes produce a new instance (<c>profile with { ... }</c>).</summary>
public sealed record RuntimeProfile
{
    /// <summary>The schema of the persisted document (SDD §8.4). Bump it together with a migrator.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Name { get; init; } = "Default";

    public string? InstallDirectory { get; init; }

    public string? GameExecutablePath { get; init; }

    public string? LauncherExecutablePath { get; init; }

    public string? UserDocumentsDirectory { get; init; }

    public LaunchTarget LaunchTarget { get; init; } = LaunchTarget.Game;

    public RuntimeMode RuntimeMode { get; init; } = RuntimeMode.OriginalGame;

    public string? ActiveModProfileName { get; init; }

    public IReadOnlyList<string> LaunchArguments { get; init; } = [];

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
