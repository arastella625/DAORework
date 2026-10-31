using DAORework.Core.Mods.Backups;

namespace DAORework.Application.Fixes;

/// <summary>The fixes DAORework applied in an install folder, whether a change is unfinished there, and its backups.</summary>
public sealed record FixStatus(IReadOnlyList<AppliedFix> Applied, bool ChangeUnfinished, IReadOnlyList<DeploymentBackup> Backups);
