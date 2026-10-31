using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Changes;

/// <summary>What DAORework has deployed in a documents folder, whether a change is unfinished, and the backups to roll back to.</summary>
public sealed record ModStatus(DeploymentRecord Record, bool ChangeUnfinished, IReadOnlyList<DeploymentBackup> Backups);
