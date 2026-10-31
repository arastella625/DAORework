using DAORework.Application.Saves;
using DAORework.Cli.Commands.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Saves;

public sealed class SavesListCommand : AsyncCommand<ProfileNameSettings>
{
    private readonly ListSaveBackups _listSaveBackups;
    private readonly SaveBackupRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public SavesListCommand(ListSaveBackups listSaveBackups, SaveBackupRenderer renderer, ErrorRenderer errors)
    {
        _listSaveBackups = listSaveBackups;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _listSaveBackups.ExecuteAsync(new ListSaveBackupsRequest(settings.Profile), cancellationToken)).TryGetValue(out var backups, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _renderer.RenderList(settings.Profile, backups);
        return ExitCodes.Success;
    }
}
