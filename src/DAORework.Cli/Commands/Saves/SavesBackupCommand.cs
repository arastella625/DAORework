using DAORework.Application.Saves;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Saves;

public sealed class SavesBackupCommand : AsyncCommand<BackupSettings>
{
    private readonly BackupSaves _backupSaves;
    private readonly SaveBackupRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public SavesBackupCommand(BackupSaves backupSaves, SaveBackupRenderer renderer, ErrorRenderer errors)
    {
        _backupSaves = backupSaves;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, BackupSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _backupSaves.ExecuteAsync(new BackupSavesRequest(settings.Profile, settings.KeepLatest), cancellationToken)).TryGetValue(out var result, out var error))
        {
            _errors.Render(error, "Backup failed");
            return ExitCodes.Failure;
        }

        _renderer.RenderCreated(result);
        return ExitCodes.Success;
    }
}
