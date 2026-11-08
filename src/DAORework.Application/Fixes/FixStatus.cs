using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Fixes;

/// <summary>The fixes DAORework applied in an install folder, whether a change is unfinished there, and its backups.</summary>
public sealed record FixStatus(IReadOnlyList<AppliedFix> Applied, bool ChangeUnfinished, IReadOnlyList<DeploymentBackup> Backups)
{
    /// <summary>Gets what is applied now, so History leaves out changes that were undone (P2.6-70).</summary>
    public DeploymentRecord? Record { get; init; }
}
