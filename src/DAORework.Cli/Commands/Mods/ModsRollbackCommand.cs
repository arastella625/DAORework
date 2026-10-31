using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsRollbackCommand : AsyncCommand<RollbackSettings>
{
    private readonly GetProfile _getProfile;
    private readonly RollBackModChange _rollBack;
    private readonly ModChangeRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsRollbackCommand(GetProfile getProfile, RollBackModChange rollBack, ModChangeRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _rollBack = rollBack;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RollbackSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var documents, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (!_renderer.Confirm(settings.Yes, $"Undo {(settings.BackupId is null ? "the most recent mod change" : $"the change backed up as {settings.BackupId}")} in {documents}?"))
        {
            return ExitCodes.Failure;
        }

        if (!(await _rollBack.ExecuteAsync(new RollBackModChangeRequest(documents, settings.BackupId), cancellationToken)).TryGetValue(out var report, out error))
        {
            _errors.Render(error, "Rollback failed");
            return ExitCodes.Failure;
        }

        _renderer.RenderRestored(report);
        return ExitCodes.Success;
    }
}
