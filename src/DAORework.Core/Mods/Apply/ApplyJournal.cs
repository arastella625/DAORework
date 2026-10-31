using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Apply;

/// <summary>
/// The executor's journal (ROADMAP P2-05), written before the first change: the plan's steps, the backup taken for it,
/// the record to save once every step is done, and the record to keep if the change is rolled back instead. Steps <c>0..Operations.Count-1</c> are the file operations; one more
/// step, when there are any, is the <c>AddIns.xml</c> edit. Which steps are done is logged separately as they finish.
/// </summary>
public sealed record ApplyJournal(
    string DocumentsDirectory,
    string BackupId,
    IReadOnlyList<PlanOperation> Operations,
    IReadOnlyList<AddInsListChange> AddInsChanges,
    DeploymentRecord Target,
    DeploymentRecord Previous)
{
    /// <summary>Version 2 adds <see cref="PlanAction.Move"/> steps and <see cref="PlanOperation.MovedFrom"/>.</summary>
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets the number of steps: one per file operation, plus one for AddIns.xml when it changes.</summary>
    public int StepCount => Operations.Count + (AddInsChanges.Count > 0 ? 1 : 0);
}
