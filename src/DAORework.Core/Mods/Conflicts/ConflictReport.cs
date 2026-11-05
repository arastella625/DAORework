using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// What activating an ordered set of mods would deploy, and every overlap found on the way. <see cref="Deploy"/> is
/// the resolved file list a plan copies: one copy per override file name, in profile order.
/// </summary>
public sealed record ConflictReport(
    IReadOnlyList<DeployedFile> Deploy,
    IReadOnlyList<OverrideConflict> OverrideConflicts,
    IReadOnlyList<PathCollision> PathCollisions,
    IReadOnlyList<UnmanagedShadow> UnmanagedShadows,
    IReadOnlyList<AddInFileOverridden> AddInFilesOverridden)
{
    /// <summary>Gets the program files the mods hold, which are never deployed (ADR-0018) and so take no part in any overlap.</summary>
    public IReadOnlyList<DeployedFile> ProgramFiles { get; init; } = [];

    /// <summary>
    /// Gets the conflicts worth telling a player about: those between mods. One mod holding a name in several folders, such
    /// as a texture beside each option, still deploys one copy, but there is nothing to choose.
    /// </summary>
    public IReadOnlyList<OverrideConflict> BetweenMods => [.. OverrideConflicts.Where(c => c.OtherModIds.Count > 0)];

    /// <summary>Gets a value indicating whether the set cannot be deployed as it is (two mods writing the same file outside override).</summary>
    public bool IsBlocked => PathCollisions.Count > 0;
}
