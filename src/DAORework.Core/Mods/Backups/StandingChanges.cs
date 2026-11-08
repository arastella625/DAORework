using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Backups;

/// <summary>
/// The backups whose changes still stand (P2.6-70), read as rollback reads them. The record names the backup of the change
/// that made it, so a newer backup belongs to a change that was undone, or that died before its journal was written. An empty
/// record that names none is the start, so none stand; a record from before records named their backup keeps them all.
/// History showed an undone round as "Changed no mods", since what came after it is what is on now. Pure.
/// </summary>
public static class StandingChanges
{
    /// <param name="newestFirst">The backups, newest first.</param>
    /// <param name="record">What is deployed now, or null to keep them all.</param>
    public static IReadOnlyList<DeploymentBackup> Of(IReadOnlyList<DeploymentBackup> newestFirst, DeploymentRecord? record)
    {
        ArgumentNullException.ThrowIfNull(newestFirst);
        if (record?.BackupId is not { } current)
        {
            return record is { ActiveModIds.Count: 0, Files.Count: 0 } ? [] : newestFirst;
        }

        var at = newestFirst.ToList().FindIndex(b => string.Equals(b.Id, current, StringComparison.OrdinalIgnoreCase));
        return at <= 0 ? newestFirst : [.. newestFirst.Skip(at)];
    }
}
