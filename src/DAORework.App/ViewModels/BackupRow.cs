using DAORework.Core.Mods.Backups;

namespace DAORework.App.ViewModels;

/// <summary>
/// One change in <i>History</i> on the Mods tab or the Fixes tab: when it was made (shown by
/// <see cref="Views.FriendlyTimeConverter"/>), what it did (P2.5-19), and what was on before it.
/// </summary>
public sealed record BackupRow(string Id, DateTimeOffset At, string Before, string? Change = null)
{
    /// <param name="backup">The backup.</param>
    /// <param name="after">What was active after its change, or null when that is not known.</param>
    /// <param name="words">How the change is worded, for mods or for fixes.</param>
    /// <param name="name">How an entry is named: a mod by its name, a fix by its name.</param>
    /// <param name="none">What is said when nothing was active before it, such as "no mods".</param>
    /// <returns>The row, or null for a backup that does not record what was active before it (one <c>status</c> leaves out too).</returns>
    public static BackupRow? From(DeploymentBackup backup, IReadOnlyList<string>? after, ChangeWords words, Func<string, string> name, string none)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(backup);
        if (backup.PreviousRecord is not { } previous)
        {
            return null;
        }

        var before = previous.ActiveModIds.Count == 0 ? none : string.Join(", ", previous.ActiveModIds.Select(name));
        var moved = backup.Entries.Any(e => e.MovedFrom is not null);
        var change = after is null ? null : DeploymentChangeText.Describe(previous.ActiveModIds, after, moved, words, name);
        return new BackupRow(backup.Id, backup.CreatedAtUtc, $"Before it: {before}", change);
    }
}
