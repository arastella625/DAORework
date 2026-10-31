using DAORework.Core.Profiles;

namespace DAORework.Core.Diagnostics;

public sealed class DiagnosticReport
{
    /// <summary>The schema of the persisted document (SDD §8.4). Bump it together with a migrator.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public string ProfileName { get; init; } = "Default";

    public bool CanLaunch => Issues.All(i => i.Severity != DiagnosticSeverity.Error);

    public IReadOnlyList<DiagnosticIssue> Issues { get; init; } = [];

    public RuntimeProfile? ProfileSnapshot { get; init; }
}
