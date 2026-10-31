using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Fixes;

/// <summary>Undoes the most recent fix change from its backup, with the same use case as mods rollback (ADR-0013).</summary>
public sealed class FixesRollbackCommand : AsyncCommand<FixesRollbackSettings>
{
    private readonly GetProfile _getProfile;
    private readonly RollBackModChange _rollBack;
    private readonly FixRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public FixesRollbackCommand(GetProfile getProfile, RollBackModChange rollBack, FixRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _rollBack = rollBack;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, FixesRollbackSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await FixesInstall.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var install, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (!_renderer.Confirm(settings.Yes, $"Undo {(settings.BackupId is null ? "the most recent fix change" : $"the change backed up as {settings.BackupId}")} in {install}?"))
        {
            return ExitCodes.Failure;
        }

        if (!(await _rollBack.ExecuteAsync(new RollBackModChangeRequest(install, settings.BackupId), cancellationToken)).TryGetValue(out var report, out error))
        {
            _errors.Render(error, "Rollback failed");
            return ExitCodes.Failure;
        }

        _renderer.RenderRestored(report);
        return ExitCodes.Success;
    }
}
