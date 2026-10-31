using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// One file change. <see cref="ExpectedSha256"/> is what the target must still hash to when the change is made (null:
/// the target must not exist), so the executor (P2-05) never touches a file that changed after the plan was shown.
/// For a <see cref="PlanAction.Move"/> it is what the moved file must hash to, and the target must not exist.
/// </summary>
public sealed record PlanOperation(PlanAction Action, string TargetPath, string ModId, DeployedFile? Source, string? ExpectedSha256)
{
    /// <summary>Gets where a <see cref="PlanAction.Move"/> takes the file from, relative to the documents folder; null for other actions.</summary>
    public string? MovedFrom { get; init; }
}
