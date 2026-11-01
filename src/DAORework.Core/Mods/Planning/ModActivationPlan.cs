using DAORework.Core.Common;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Conflicts;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// Everything that activating a list of mods would change in one documents folder, worked out before anything is
/// touched (SDD §5, Plan → Apply; ROADMAP P2-03). It is shown to the user, confirmed, backed up (P2-04) and applied
/// by the journaled executor (P2-05). A plan with blockers is never applied.
/// </summary>
public sealed record ModActivationPlan(
    IReadOnlyList<string> CurrentModIds,
    IReadOnlyList<string> TargetModIds,
    IReadOnlyList<PlanOperation> Operations,
    IReadOnlyList<AddInsListChange> AddInsChanges,
    int UnchangedFileCount,
    IReadOnlyList<string> MissingRecordedFiles,
    IReadOnlyList<PlanBlocker> Blockers,
    ConflictReport Conflicts)
{
    /// <summary>Gets the deployment record the plan was made from, which is kept again if the change is rolled back.</summary>
    public DeploymentRecord CurrentRecord { get; init; } = DeploymentRecord.Empty;

    /// <summary>Gets the deployment record that describes the folder once the plan has been applied.</summary>
    public DeploymentRecord NextRecord { get; init; } = DeploymentRecord.Empty;

    /// <summary>Gets the files an adoption takes over where they already are, which it only records (ADR-0015).</summary>
    public int AdoptedInPlaceCount { get; init; }

    /// <summary>Gets the files the plan deploys combined from several mods' copies (ADR-0025).</summary>
    public IReadOnlyList<CombinedOverride> Combined { get; init; } = [];

    /// <summary>Gets why copies that would have been combined were not; the copy from the mod last in the load order is used.</summary>
    public IReadOnlyList<string> CombineProblems { get; init; } = [];

    /// <summary>Gets a value indicating whether the plan must not be applied until its blockers are resolved.</summary>
    public bool IsBlocked => Blockers.Count > 0;

    /// <summary>Gets a value indicating whether applying the plan would change nothing: no file, no <c>AddIns.xml</c> entry, nothing adopted.</summary>
    public bool IsEmpty => Operations.Count == 0 && AddInsChanges.Count == 0 && AdoptedInPlaceCount == 0;

    /// <summary>Gets the bytes the copies and replacements write, which must be free on the documents folder's drive.</summary>
    public ByteSize BytesToWrite => new(Operations.Where(o => o.Source is not null).Sum(o => o.Source!.Source.SizeBytes));

    public int Count(PlanAction action) => Operations.Count(o => o.Action == action);
}
