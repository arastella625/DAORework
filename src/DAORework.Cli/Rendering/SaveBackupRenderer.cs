using System.Globalization;
using DAORework.Application.Saves;
using DAORework.Core.Common;
using DAORework.Core.Saves;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class SaveBackupRenderer
{
    private readonly IAnsiConsole _console;

    public SaveBackupRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderCreated(SaveBackupResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _console.WriteLine($"Backed up {result.FileCount} file(s), {new ByteSize(result.SourceBytes)}, to: {result.Backup.Path}");
        foreach (var deleted in result.Deleted)
        {
            _console.WriteLine($"Removed old backup: {deleted.Path}");
        }
    }

    public void RenderList(string profileName, IReadOnlyList<SaveBackup> backups)
    {
        ArgumentNullException.ThrowIfNull(backups);
        if (backups.Count == 0)
        {
            _console.WriteLine($"No save backups for '{profileName}' yet. Run: saves backup {profileName}");
            return;
        }

        foreach (var backup in backups)
        {
            _console.WriteLine($"{backup.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}  {new ByteSize(backup.SizeBytes),10}  {backup.Path}");
        }
    }
}
