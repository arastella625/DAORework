using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Backups;

/// <summary>
/// The memento for one plan (SDD §5, ROADMAP P2-04): what every touched path in the documents folder held before
/// the plan ran. Stored under the app-data folder; restoring it returns those paths to that state.
/// </summary>
public sealed record DeploymentBackup(string Id, DateTimeOffset CreatedAtUtc, string DocumentsDirectory, IReadOnlyList<BackupEntry> Entries)
{
    /// <summary>Version 2 adds <see cref="BackupEntry.MovedFrom"/>; a build before it would delete a moved file on rollback.</summary>
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets the deployment record from before the change, which a rollback puts back with the files.</summary>
    public DeploymentRecord? PreviousRecord { get; init; }
}
