using System.Globalization;
using DAORework.Core.Mods.Backups;

namespace DAORework.App.ViewModels;

/// <summary>One backup on the Mods tab's <i>Changes</i> panel: when the change it undoes was made, and what was active before it.</summary>
public sealed record BackupRow(string Id, string When, string Before)
{
    /// <param name="backup">The backup.</param>
    /// <param name="name">How an entry active before it is named: a mod by its id, a fix by its name.</param>
    /// <param name="none">What is said when nothing was active before it, such as "no mods".</param>
    /// <returns>The row, or null for a backup that does not record what was active before it (one <c>status</c> leaves out too).</returns>
    public static BackupRow? From(DeploymentBackup backup, Func<string, string> name, string none)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(backup);
        if (backup.PreviousRecord is not { } previous)
        {
            return null;
        }

        var when = backup.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var before = previous.ActiveModIds.Count == 0 ? none : string.Join(", ", previous.ActiveModIds.Select(name));
        return new BackupRow(backup.Id, when, $"Before it: {before}");
    }
}
