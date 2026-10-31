namespace DAORework.Core.Mods.Planning;

/// <summary>
/// What DAORework has deployed into one documents folder (ADR-0012): the active mods in order, every file it placed
/// with its hash at the time, and the add-ins it added to <c>Settings\AddIns.xml</c>. Only what is in here is
/// ever deleted or overwritten, and only while its hash still matches.
/// </summary>
public sealed record DeploymentRecord(
    IReadOnlyList<string> ActiveModIds,
    IReadOnlyList<RecordedFile> Files,
    IReadOnlyList<RecordedAddIn> AddedAddIns)
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets the id of the backup taken before the change that produced this record, which is what a rollback restores.
    /// Null for the empty record and for records written before rollbacks followed it.
    /// </summary>
    public string? BackupId { get; init; }

    /// <summary>Gets the record of a documents folder DAORework has never deployed into.</summary>
    public static DeploymentRecord Empty { get; } = new([], [], []);
}
