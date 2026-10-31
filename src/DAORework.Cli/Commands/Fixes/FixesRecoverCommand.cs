using DAORework.Application.Mods.Apply;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Apply;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Fixes;

/// <summary>Settles a fix change that was interrupted, with the same use case as mods recover (ADR-0013).</summary>
public sealed class FixesRecoverCommand : AsyncCommand<FixesRecoverSettings>
{
    private readonly GetProfile _getProfile;
    private readonly RecoverModPlan _recover;
    private readonly FixRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public FixesRecoverCommand(GetProfile getProfile, RecoverModPlan recover, FixRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _recover = recover;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, FixesRecoverSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await FixesInstall.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var install, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        var choice = settings.Finish ? RecoveryChoice.Finish : RecoveryChoice.RollBack;
        if (!(await _recover.ExecuteAsync(new RecoverModPlanRequest(install, choice), cancellationToken)).TryGetValue(out var report, out error))
        {
            _errors.Render(error, "Recovery failed");
            return ExitCodes.Failure;
        }

        _renderer.Say(choice == RecoveryChoice.Finish
            ? $"Finished the interrupted change: {report.StepsRun} step(s) run, {report.StepsAlreadyDone} were already done."
            : $"Rolled the interrupted change back to backup {report.BackupId}.");
        return ExitCodes.Success;
    }
}
