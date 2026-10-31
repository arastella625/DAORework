using DAORework.Application.Mods.Apply;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Apply;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsRecoverCommand : AsyncCommand<RecoverSettings>
{
    private readonly GetProfile _getProfile;
    private readonly RecoverModPlan _recover;
    private readonly ModChangeRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsRecoverCommand(GetProfile getProfile, RecoverModPlan recover, ModChangeRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _recover = recover;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RecoverSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var documents, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        var choice = settings.Finish ? RecoveryChoice.Finish : RecoveryChoice.RollBack;
        if (!(await _recover.ExecuteAsync(new RecoverModPlanRequest(documents, choice), cancellationToken)).TryGetValue(out var report, out error))
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
