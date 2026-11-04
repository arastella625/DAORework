using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Changes;

/// <param name="DocumentsDirectory">The documents folder the change is made in.</param>
/// <param name="Change">The change.</param>
/// <param name="InstallDirectory">The install folder, whose override the plan's warnings look at (P2.5-22), if known.</param>
public sealed record PlanModChangeRequest(string DocumentsDirectory, ModChange Change, string? InstallDirectory = null)
{
    /// <summary>Gets the official DLC entries the plan puts back in <c>AddIns.xml</c> (ADR-0028 §3).</summary>
    public IReadOnlyList<AddInsListChange> RestoredAddIns { get; init; } = [];
}
